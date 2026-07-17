using System.Buffers.Binary;
using System.IO.Compression;
using System.Text.Json;

namespace Zircon.AssetPipeline;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 0 || HasArg(args, "--help"))
        {
            PrintUsage();
            return 0;
        }

        string source = GetArg(args, "--source") ?? throw new ArgumentException("Missing --source.");
        string output = GetArg(args, "--output") ?? Path.Combine("Assets", "Generated", "Textures");
        string type = GetArg(args, "--type") ?? "image";
        string? rectText = GetArg(args, "--rect");
        ZirconMapRect? rect = ParseRect(rectText);
        int max = int.TryParse(GetArg(args, "--max"), out int parsedMax) ? parsedMax : rect.HasValue ? 4096 : 16;
        HashSet<int>? indices = ParseIndices(GetArg(args, "--indices"));

        Directory.CreateDirectory(output);

        if (string.Equals(Path.GetExtension(source), ".db", StringComparison.OrdinalIgnoreCase))
        {
            SystemDbContent content = SystemDbMagicReader.ReadContent(source);
            SystemDbMagicReader.WriteContentManifests(source, output, content);
            Console.WriteLine($"system manifests={output} magics={content.Magics.Count} items={content.Items.Count} npcPages={content.NpcPages.Count} npcButtons={content.NpcButtons.Count} npcGoods={content.NpcGoods.Count} quests={content.Quests.Count} questTasks={content.QuestTasks.Count} maps={content.Maps.Count}");
            return 0;
        }

        if (string.Equals(Path.GetExtension(source), ".map", StringComparison.OrdinalIgnoreCase))
        {
            ZirconMapManifest map = ZirconMapReader.Read(source, max, rect);
            string mapManifestPath = Path.Combine(output, $"{Path.GetFileNameWithoutExtension(source)}.map.manifest.json");
            File.WriteAllText(mapManifestPath, JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"map manifest={mapManifestPath} size={map.Width}x{map.Height} view={map.ViewX},{map.ViewY},{map.ViewWidth},{map.ViewHeight} sampledCells={map.SampleCells.Count}");
            return 0;
        }

        using var library = ZirconZlLibrary.Open(source);
        string manifestPath = Path.Combine(output, $"{Path.GetFileNameWithoutExtension(source)}.manifest.json");
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(library.Images, new JsonSerializerOptions { WriteIndented = true }));

        int exported = 0;
        foreach (ZirconZlImage image in library.Images)
        {
            if (!image.Exists)
                continue;

            if (indices != null && !indices.Contains(image.Index))
                continue;

            if (indices == null && exported >= max)
                break;

            if (!library.TryReadDxt1(image, type, out int width, out int height, out byte[] dxt))
                continue;

            byte[] rgba = Dxt1Decoder.Decode(width, height, dxt);
            string pngPath = Path.Combine(output, $"{Path.GetFileNameWithoutExtension(source)}_{image.Index:D5}_{type}.png");
            PngWriter.WriteRgba32(pngPath, width, height, rgba);
            exported++;
            Console.WriteLine($"exported index={image.Index} type={type} size={width}x{height} path={pngPath}");
        }

        Console.WriteLine($"manifest={manifestPath} images={library.Images.Count} exported={exported}");
        return 0;
    }

    private static bool HasArg(string[] args, string name)
    {
        return args.Any(arg => string.Equals(arg, name, StringComparison.OrdinalIgnoreCase));
    }

    private static string? GetArg(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        return null;
    }

    private static HashSet<int>? ParseIndices(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var values = new HashSet<int>();
        foreach (string part in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, out int value))
                values.Add(value);
        }

        return values;
    }

    private static ZirconMapRect? ParseRect(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        string[] parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 4)
            throw new ArgumentException("--rect must be x,y,width,height.");

        if (!int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int y) || !int.TryParse(parts[2], out int width) || !int.TryParse(parts[3], out int height))
            throw new ArgumentException("--rect must contain integers.");

        if (width <= 0 || height <= 0)
            throw new ArgumentException("--rect width and height must be positive.");

        return new ZirconMapRect(x, y, width, height);
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Zircon.AssetPipeline");
        Console.WriteLine("  --source <Data\\Inventory.Zl> --output <dir> [--type image|shadow|overlay] [--indices 0,1,2] [--max 16]");
        Console.WriteLine("  --source <Map\\0.map> --output <dir> [--max 64] [--rect x,y,width,height]");
    }
}


internal static class ZirconMapReader
{
    public static ZirconMapManifest Read(string path, int maxSampleCells, ZirconMapRect? exportRect)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);

        stream.Seek(22, SeekOrigin.Begin);
        short width = reader.ReadInt16();
        short height = reader.ReadInt16();
        stream.Seek(28, SeekOrigin.Begin);

        var cells = new ZirconMapCell[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
                cells[x, y] = new ZirconMapCell(x, y, 0, 0, 0, 0, 0, 0, 0, 0, 0, false);
        }

        for (int x = 0; x < width / 2; x++)
        {
            for (int y = 0; y < height / 2; y++)
            {
                ZirconMapCell existing = cells[x * 2, y * 2];
                cells[x * 2, y * 2] = existing with
                {
                    BackFile = reader.ReadByte(),
                    BackImage = reader.ReadUInt16()
                };
            }
        }

        int blockingCells = 0;
        int nonEmptyCells = 0;
        var sampleCells = new List<ZirconMapCell>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                byte flag = reader.ReadByte();
                byte middleAnimationFrame = reader.ReadByte();
                byte value = reader.ReadByte();
                byte frontAnimationFrame = value == 255 ? (byte)0 : value;
                frontAnimationFrame &= 0x8F;

                byte frontFile = reader.ReadByte();
                byte middleFile = reader.ReadByte();
                ushort middleImage = (ushort)(reader.ReadUInt16() + 1);
                ushort frontImage = (ushort)(reader.ReadUInt16() + 1);

                stream.Seek(3, SeekOrigin.Current);
                byte light = (byte)((reader.ReadByte() & 0x0F) * 2);
                stream.Seek(1, SeekOrigin.Current);

                bool blocking = ((flag & 0x01) != 1) || ((flag & 0x02) != 2);
                if (blocking)
                    blockingCells++;

                ZirconMapCell existing = cells[x, y];
                var cell = existing with
                {
                    MiddleAnimationFrame = middleAnimationFrame,
                    FrontAnimationFrame = frontAnimationFrame,
                    FrontFile = frontFile,
                    MiddleFile = middleFile,
                    MiddleImage = middleImage,
                    FrontImage = frontImage,
                    Light = light,
                    Blocking = blocking
                };
                cells[x, y] = cell;

                bool nonEmpty = HasLayer(cell.BackFile, cell.BackImage) || HasLayer(cell.MiddleFile, cell.MiddleImage) || HasLayer(cell.FrontFile, cell.FrontImage);
                if (nonEmpty)
                    nonEmptyCells++;

                if (!IsInExportWindow(cell, exportRect))
                    continue;

                if (!exportRect.HasValue && !nonEmpty)
                    continue;

                if (maxSampleCells <= 0 || sampleCells.Count < maxSampleCells)
                    sampleCells.Add(cell);
            }
        }

        ZirconMapRect view = exportRect ?? new ZirconMapRect(0, 0, width, height);
        return new ZirconMapManifest(Path.GetFileName(path), width, height, blockingCells, nonEmptyCells, view.X, view.Y, view.Width, view.Height, sampleCells);
    }

    private static bool IsInExportWindow(ZirconMapCell cell, ZirconMapRect? rect)
    {
        if (!rect.HasValue)
            return true;

        ZirconMapRect value = rect.Value;
        return cell.X >= value.X && cell.Y >= value.Y && cell.X < value.X + value.Width && cell.Y < value.Y + value.Height;
    }

    private static bool HasLayer(int file, int image)
    {
        return file != 0 && file != 255 && image != 0;
    }
}

internal sealed record ZirconMapManifest(
    string Source,
    int Width,
    int Height,
    int BlockingCells,
    int NonEmptyCells,
    int ViewX,
    int ViewY,
    int ViewWidth,
    int ViewHeight,
    IReadOnlyList<ZirconMapCell> SampleCells);

internal readonly record struct ZirconMapRect(int X, int Y, int Width, int Height);

internal sealed record ZirconMapCell(
    int X,
    int Y,
    int BackFile,
    int BackImage,
    int MiddleFile,
    int MiddleImage,
    int FrontFile,
    int FrontImage,
    int MiddleAnimationFrame,
    int FrontAnimationFrame,
    int Light,
    bool Blocking);
internal sealed class ZirconZlLibrary : IDisposable
{
    private readonly FileStream stream;

    private ZirconZlLibrary(FileStream stream, IReadOnlyList<ZirconZlImage> images)
    {
        this.stream = stream;
        Images = images;
    }

    public IReadOnlyList<ZirconZlImage> Images { get; }

    public static ZirconZlLibrary Open(string path)
    {
        var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        int headerLength = reader.ReadInt32();
        byte[] header = reader.ReadBytes(headerLength);

        using var headerStream = new MemoryStream(header);
        using var headerReader = new BinaryReader(headerStream);
        int count = headerReader.ReadInt32();
        var images = new List<ZirconZlImage>(count);

        for (int i = 0; i < count; i++)
        {
            bool exists = headerReader.ReadBoolean();
            if (!exists)
            {
                images.Add(ZirconZlImage.Empty(i));
                continue;
            }

            images.Add(new ZirconZlImage(
                i,
                true,
                headerReader.ReadInt32(),
                headerReader.ReadInt16(),
                headerReader.ReadInt16(),
                headerReader.ReadInt16(),
                headerReader.ReadInt16(),
                headerReader.ReadByte(),
                headerReader.ReadInt16(),
                headerReader.ReadInt16(),
                headerReader.ReadInt16(),
                headerReader.ReadInt16(),
                headerReader.ReadInt16(),
                headerReader.ReadInt16()));
        }

        return new ZirconZlLibrary(stream, images);
    }

    public bool TryReadDxt1(ZirconZlImage image, string type, out int width, out int height, out byte[] bytes)
    {
        width = 0;
        height = 0;
        bytes = Array.Empty<byte>();

        if (!image.Exists || image.Position <= 0)
            return false;

        int offset = image.Position;
        switch (type.ToLowerInvariant())
        {
            case "image":
                width = image.Width;
                height = image.Height;
                break;
            case "shadow":
                width = image.ShadowWidth;
                height = image.ShadowHeight;
                offset += image.ImageDataSize;
                break;
            case "overlay":
                width = image.OverlayWidth;
                height = image.OverlayHeight;
                offset += image.ImageDataSize + image.ShadowDataSize;
                break;
            default:
                throw new ArgumentException($"Unsupported type: {type}");
        }

        int size = ZirconZlImage.GetDxt1Size(width, height);
        if (width <= 0 || height <= 0 || size <= 0)
            return false;

        stream.Seek(offset, SeekOrigin.Begin);
        bytes = new byte[size];
        int read = stream.Read(bytes, 0, bytes.Length);
        return read == bytes.Length;
    }

    public void Dispose()
    {
        stream.Dispose();
    }
}

internal sealed record ZirconZlImage(
    int Index,
    bool Exists,
    int Position,
    short Width,
    short Height,
    short OffSetX,
    short OffSetY,
    byte ShadowType,
    short ShadowWidth,
    short ShadowHeight,
    short ShadowOffSetX,
    short ShadowOffSetY,
    short OverlayWidth,
    short OverlayHeight)
{
    public int ImageDataSize => GetDxt1Size(Width, Height);
    public int ShadowDataSize => GetDxt1Size(ShadowWidth, ShadowHeight);
    public int OverlayDataSize => GetDxt1Size(OverlayWidth, OverlayHeight);

    public static ZirconZlImage Empty(int index)
    {
        return new ZirconZlImage(index, false, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    }

    public static int GetDxt1Size(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return 0;

        int w = width + (4 - width % 4) % 4;
        int h = height + (4 - height % 4) % 4;
        return w * h / 2;
    }
}

internal static class Dxt1Decoder
{
    public static byte[] Decode(int width, int height, byte[] dxt)
    {
        int paddedWidth = width + (4 - width % 4) % 4;
        int paddedHeight = height + (4 - height % 4) % 4;
        byte[] rgba = new byte[width * height * 4];
        int source = 0;

        for (int by = 0; by < paddedHeight; by += 4)
        {
            for (int bx = 0; bx < paddedWidth; bx += 4)
            {
                if (source + 8 > dxt.Length)
                    return rgba;

                ushort c0 = BinaryPrimitives.ReadUInt16LittleEndian(dxt.AsSpan(source, 2));
                ushort c1 = BinaryPrimitives.ReadUInt16LittleEndian(dxt.AsSpan(source + 2, 2));
                uint bits = BinaryPrimitives.ReadUInt32LittleEndian(dxt.AsSpan(source + 4, 4));
                source += 8;

                Rgba[] colors = BuildPalette(c0, c1);
                for (int py = 0; py < 4; py++)
                {
                    for (int px = 0; px < 4; px++)
                    {
                        int x = bx + px;
                        int y = by + py;
                        int code = (int)((bits >> (2 * (py * 4 + px))) & 0x03);

                        if (x >= width || y >= height)
                            continue;

                        Rgba color = colors[code];
                        int target = (y * width + x) * 4;
                        rgba[target] = color.R;
                        rgba[target + 1] = color.G;
                        rgba[target + 2] = color.B;
                        rgba[target + 3] = color.A;
                    }
                }
            }
        }

        return rgba;
    }

    private static Rgba[] BuildPalette(ushort c0, ushort c1)
    {
        Rgba a = From565(c0);
        Rgba b = From565(c1);
        var colors = new Rgba[4];
        colors[0] = a;
        colors[1] = b;

        if (c0 > c1)
        {
            colors[2] = Lerp(a, b, 2, 1, 3);
            colors[3] = Lerp(a, b, 1, 2, 3);
        }
        else
        {
            colors[2] = Lerp(a, b, 1, 1, 2);
            colors[3] = new Rgba(0, 0, 0, 0);
        }

        return colors;
    }

    private static Rgba From565(ushort value)
    {
        byte r = (byte)(((value >> 11) & 0x1F) * 255 / 31);
        byte g = (byte)(((value >> 5) & 0x3F) * 255 / 63);
        byte b = (byte)((value & 0x1F) * 255 / 31);
        return new Rgba(r, g, b, 255);
    }

    private static Rgba Lerp(Rgba a, Rgba b, int aw, int bw, int divisor)
    {
        return new Rgba(
            (byte)((a.R * aw + b.R * bw) / divisor),
            (byte)((a.G * aw + b.G * bw) / divisor),
            (byte)((a.B * aw + b.B * bw) / divisor),
            255);
    }

    private readonly record struct Rgba(byte R, byte G, byte B, byte A);
}

internal static class PngWriter
{
    private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };

    public static void WriteRgba32(string path, int width, int height, byte[] rgba)
    {
        using var output = File.Create(path);
        output.Write(Signature);
        WriteChunk(output, "IHDR", BuildHeader(width, height));
        WriteChunk(output, "IDAT", Compress(BuildScanlines(width, height, rgba)));
        WriteChunk(output, "IEND", Array.Empty<byte>());
    }

    private static byte[] BuildHeader(int width, int height)
    {
        byte[] data = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(0, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(4, 4), height);
        data[8] = 8;
        data[9] = 6;
        return data;
    }

    private static byte[] BuildScanlines(int width, int height, byte[] rgba)
    {
        int stride = width * 4;
        byte[] data = new byte[(stride + 1) * height];
        for (int y = 0; y < height; y++)
        {
            int target = y * (stride + 1);
            data[target] = 0;
            Buffer.BlockCopy(rgba, y * stride, data, target + 1, stride);
        }

        return data;
    }

    private static byte[] Compress(byte[] data)
    {
        using var memory = new MemoryStream();
        using (var zlib = new ZLibStream(memory, CompressionLevel.Optimal, leaveOpen: true))
            zlib.Write(data, 0, data.Length);
        return memory.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        stream.Write(length);

        byte[] typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        stream.Write(typeBytes);
        stream.Write(data);

        uint crc = Crc32(typeBytes, data);
        Span<byte> crcBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc);
        stream.Write(crcBytes);
    }

    private static uint Crc32(byte[] type, byte[] data)
    {
        uint crc = 0xffffffff;
        foreach (byte value in type)
            crc = Update(crc, value);
        foreach (byte value in data)
            crc = Update(crc, value);
        return crc ^ 0xffffffff;
    }

    private static uint Update(uint crc, byte value)
    {
        crc ^= value;
        for (int i = 0; i < 8; i++)
            crc = (crc & 1) != 0 ? 0xedb88320 ^ (crc >> 1) : crc >> 1;
        return crc;
    }
}

