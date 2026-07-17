using System.Collections;
using System.Text.Json;

namespace Zircon.AssetPipeline;

internal static class SystemDbMagicReader
{
    public static IReadOnlyList<MagicManifestEntry> Read(string path)
    {
        return ReadContent(path).Magics;
    }

    public static SystemDbContent ReadContent(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);

        int mappingCount = ReadCount(reader, "mapping");
        var mappings = new List<DbMapping>(mappingCount);
        for (int i = 0; i < mappingCount; i++)
        {
            string typeName = reader.ReadString();
            int propertyCount = ReadCount(reader, "property");
            var properties = new List<DbProperty>(propertyCount);
            for (int p = 0; p < propertyCount; p++)
                properties.Add(new DbProperty(reader.ReadString(), reader.ReadString()));
            mappings.Add(new DbMapping(typeName, properties));
        }

        var magics = new List<MagicManifestEntry>();
        var items = new List<ItemManifestEntry>();
        var npcPages = new List<NpcPageManifestEntry>();
        var npcButtons = new List<NpcButtonManifestEntry>();
        var npcGoods = new List<NpcGoodManifestEntry>();
        var quests = new List<QuestManifestEntry>();
        var questTasks = new List<QuestTaskManifestEntry>();
        var maps = new List<MapManifestEntry>();

        foreach (DbMapping mapping in mappings)
        {
            int dataLength = reader.ReadInt32();
            if (dataLength < 0 || dataLength > stream.Length - stream.Position)
                throw new InvalidDataException($"Invalid collection length {dataLength} for {mapping.TypeName}.");

            byte[] data = reader.ReadBytes(dataLength);
            if (mapping.TypeName.EndsWith(".MagicInfo", StringComparison.Ordinal))
            {
                ReadMagicCollection(data, mapping, magics);
            }
            else if (mapping.TypeName.EndsWith(".ItemInfo", StringComparison.Ordinal))
            {
                ReadCollection(data, mapping, values => new ItemManifestEntry(
                    GetInt(values, "Index"), GetString(values, "ItemName"), GetInt(values, "ItemType"),
                    GetInt(values, "Image"), GetInt(values, "Durability"), GetInt(values, "Price"),
                    GetInt(values, "Weight"), GetInt(values, "StackSize"), GetBool(values, "CanRepair"),
                    GetBool(values, "CanSell"), GetBool(values, "CanStore"), GetBool(values, "CanTrade"),
                    GetBool(values, "CanDrop"), GetString(values, "Description"), GetInt(values, "Rarity")), items);
            }
            else if (mapping.TypeName.EndsWith(".QuestInfo", StringComparison.Ordinal))
            {
                ReadCollection(data, mapping, values => new QuestManifestEntry(
                    GetInt(values, "Index"), GetString(values, "QuestName"), GetString(values, "AcceptText"),
                    GetString(values, "ProgressText"), GetString(values, "CompletedText"), GetString(values, "ArchiveText"),
                    GetInt(values, "StartNPC"), GetInt(values, "FinishNPC")), quests);
            }
            else if (mapping.TypeName.EndsWith(".QuestTask", StringComparison.Ordinal))
            {
                ReadCollection(data, mapping, values => new QuestTaskManifestEntry(
                    GetInt(values, "Index"), GetInt(values, "Quest"), GetInt(values, "Task"),
                    GetInt(values, "ItemParameter"), GetString(values, "MobDescription"), GetInt(values, "Amount")), questTasks);
            }
            else if (mapping.TypeName.EndsWith(".MapInfo", StringComparison.Ordinal))
            {
                ReadCollection(data, mapping, values => new MapManifestEntry(
                    GetInt(values, "Index"), GetString(values, "FileName"), GetString(values, "Description"), GetInt(values, "MiniMap")), maps);
            }            else if (mapping.TypeName.EndsWith(".NPCPage", StringComparison.Ordinal))
            {
                ReadCollection(data, mapping, values => new NpcPageManifestEntry(
                    GetInt(values, "Index"), GetString(values, "Description"), GetInt(values, "DialogType"),
                    GetString(values, "Say"), GetInt(values, "SuccessPage"), GetString(values, "Arguments")), npcPages);
            }
            else if (mapping.TypeName.EndsWith(".NPCGood", StringComparison.Ordinal))
            {
                ReadCollection(data, mapping, values => new NpcGoodManifestEntry(
                    GetInt(values, "Index"), GetInt(values, "Page"), GetInt(values, "Item"),
                    GetDecimal(values, "Rate")), npcGoods);
            }
            else if (mapping.TypeName.EndsWith(".NPCButton", StringComparison.Ordinal))
            {
                ReadCollection(data, mapping, values => new NpcButtonManifestEntry(
                    GetInt(values, "Index"), GetInt(values, "Page"), GetInt(values, "ButtonID"),
                    GetInt(values, "DestinationPage")), npcButtons);
            }
        }

        return new SystemDbContent(magics, items, npcPages, npcButtons, npcGoods, quests, questTasks, maps);
    }

    public static void WriteContentManifests(string source, string outputDirectory, SystemDbContent content)
    {
        Directory.CreateDirectory(outputDirectory);
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(Path.Combine(outputDirectory, "magics.manifest.json"), JsonSerializer.Serialize(new MagicManifest(Path.GetFileName(source), DateTime.UtcNow, content.Magics), options));
        File.WriteAllText(Path.Combine(outputDirectory, "items.manifest.json"), JsonSerializer.Serialize(new ItemManifest(Path.GetFileName(source), DateTime.UtcNow, content.Items), options));
        File.WriteAllText(Path.Combine(outputDirectory, "npc-pages.manifest.json"), JsonSerializer.Serialize(new NpcPageManifest(Path.GetFileName(source), DateTime.UtcNow, content.NpcPages, content.NpcButtons, content.NpcGoods), options));
        File.WriteAllText(Path.Combine(outputDirectory, "quests.manifest.json"), JsonSerializer.Serialize(new QuestManifest(Path.GetFileName(source), DateTime.UtcNow, content.Quests, content.QuestTasks), options));
        File.WriteAllText(Path.Combine(outputDirectory, "maps.manifest.json"), JsonSerializer.Serialize(new MapInfoManifest(Path.GetFileName(source), DateTime.UtcNow, content.Maps), options));
    }

    public static void WriteManifest(string source, string outputPath, IReadOnlyList<MagicManifestEntry> entries)
    {
        var manifest = new MagicManifest(Path.GetFileName(source), DateTime.UtcNow, entries);
        File.WriteAllText(outputPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void ReadMagicCollection(byte[] data, DbMapping mapping, ICollection<MagicManifestEntry> output)
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);

        int highestIndex = reader.ReadInt32();
        int count = ReadCount(reader, "magic object");
        for (int i = 0; i < count; i++)
        {
            int rawLength = reader.ReadInt32();
            if (rawLength < 0 || rawLength > stream.Length - stream.Position)
                throw new InvalidDataException($"Invalid MagicInfo object length: {rawLength}.");

            byte[] raw = reader.ReadBytes(rawLength);
            Dictionary<string, object?> values = ReadObject(raw, mapping);
            output.Add(new MagicManifestEntry(
                GetInt(values, "Index"),
                GetString(values, "Name"),
                GetInt(values, "Magic"),
                GetInt(values, "Class"),
                GetInt(values, "School"),
                GetInt(values, "Mode"),
                GetInt(values, "Icon"),
                GetInt(values, "BaseCost"),
                GetInt(values, "LevelCost"),
                GetInt(values, "Delay"),
                GetString(values, "Description")));
        }

        if (count > 0 && output.Max(x => x.Index) > highestIndex)
            throw new InvalidDataException("MagicInfo collection index metadata is inconsistent.");
    }

    private static void ReadCollection<T>(byte[] data, DbMapping mapping, Func<Dictionary<string, object?>, T> convert, ICollection<T> output)
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);
        reader.ReadInt32(); // Highest index.
        int count = ReadCount(reader, "database object");
        for (int i = 0; i < count; i++)
        {
            int rawLength = reader.ReadInt32();
            if (rawLength < 0 || rawLength > stream.Length - stream.Position)
                throw new InvalidDataException($"Invalid {mapping.TypeName} object length: {rawLength}.");
            output.Add(convert(ReadObject(reader.ReadBytes(rawLength), mapping)));
        }
    }
    private static Dictionary<string, object?> ReadObject(byte[] raw, DbMapping mapping)
    {
        using var stream = new MemoryStream(raw);
        using var reader = new BinaryReader(stream);
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (DbProperty property in mapping.Properties)
            values[property.Name] = ReadValue(reader, property.TypeName);

        if (stream.Position != stream.Length)
            throw new InvalidDataException($"MagicInfo object has {stream.Length - stream.Position} unread bytes.");

        return values;
    }

    private static object? ReadValue(BinaryReader reader, string typeName)
    {
        return typeName switch
        {
            "System.Boolean" => reader.ReadBoolean(),
            "System.Byte" => reader.ReadByte(),
            "System.Byte[]" => reader.ReadBytes(ReadCount(reader, "byte array")),
            "System.Char" => reader.ReadChar(),
            "System.Drawing.Color" => reader.ReadInt32(),
            "System.DateTime" => reader.ReadInt64(),
            "System.Decimal" => reader.ReadDecimal(),
            "System.Double" => reader.ReadDouble(),
            "System.Int16" => reader.ReadInt16(),
            "System.Int32" => reader.ReadInt32(),
            "System.Int32[]" => ReadIntArray(reader),
            "System.Int64" => reader.ReadInt64(),
            "System.Drawing.Point" => new[] { reader.ReadInt32(), reader.ReadInt32() },
            "System.SByte" => reader.ReadSByte(),
            "System.Single" => reader.ReadSingle(),
            "System.Drawing.Size" => new[] { reader.ReadInt32(), reader.ReadInt32() },
            "System.String" => reader.ReadString(),
            "System.TimeSpan" => reader.ReadInt64(),
            "System.UInt16" => reader.ReadUInt16(),
            "System.UInt32" => reader.ReadUInt32(),
            "System.UInt64" => reader.ReadUInt64(),
            "System.Drawing.Point[]" => ReadPointArray(reader),
            "Library.Stats" => ReadStats(reader),
            "System.Collections.BitArray" => ReadBitArray(reader),
            _ => throw new InvalidDataException($"Unsupported System.db property type: {typeName}.")
        };
    }

    private static int[]? ReadIntArray(BinaryReader reader)
    {
        if (!reader.ReadBoolean())
            return null;

        int count = ReadCount(reader, "int array");
        var values = new int[count];
        for (int i = 0; i < count; i++)
            values[i] = reader.ReadInt32();

        return values;
    }

    private static int[][]? ReadPointArray(BinaryReader reader)
    {
        if (!reader.ReadBoolean())
            return null;

        int count = ReadCount(reader, "point array");
        var values = new int[count][];
        for (int i = 0; i < count; i++)
            values[i] = new[] { reader.ReadInt32(), reader.ReadInt32() };

        return values;
    }

    private static Dictionary<int, int>? ReadStats(BinaryReader reader)
    {
        if (!reader.ReadBoolean())
            return null;

        int count = ReadCount(reader, "stats");
        var values = new Dictionary<int, int>(count);
        for (int i = 0; i < count; i++)
            values[reader.ReadInt32()] = reader.ReadInt32();

        return values;
    }

    private static byte[]? ReadBitArray(BinaryReader reader)
    {
        if (!reader.ReadBoolean())
            return null;

        return reader.ReadBytes(ReadCount(reader, "bit array"));
    }

    private static int ReadCount(BinaryReader reader, string name)
    {
        int count = reader.ReadInt32();
        if (count < 0 || count > 10000000)
            throw new InvalidDataException($"Invalid {name} count: {count}.");

        return count;
    }

    private static int GetInt(IReadOnlyDictionary<string, object?> values, string name)
    {
        return values.TryGetValue(name, out object? value) && value != null ? Convert.ToInt32(value) : 0;
    }

    private static decimal GetDecimal(IReadOnlyDictionary<string, object?> values, string name)
    {
        return values.TryGetValue(name, out object? value) && value != null ? Convert.ToDecimal(value) : 0M;
    }
    private static bool GetBool(IReadOnlyDictionary<string, object?> values, string name)
    {
        return values.TryGetValue(name, out object? value) && value != null && Convert.ToBoolean(value);
    }
    private static string GetString(IReadOnlyDictionary<string, object?> values, string name)
    {
        return values.TryGetValue(name, out object? value) ? value?.ToString() ?? string.Empty : string.Empty;
    }

    private sealed record DbMapping(string TypeName, IReadOnlyList<DbProperty> Properties);
    private sealed record DbProperty(string Name, string TypeName);
}

internal sealed record MagicManifest(string Source, DateTime GeneratedUtc, IReadOnlyList<MagicManifestEntry> Magics);
internal sealed record MagicManifestEntry(int Index, string Name, int MagicType, int CharacterClass, int School, int Mode, int Icon, int BaseCost, int LevelCost, int Delay, string Description);internal sealed record SystemDbContent(IReadOnlyList<MagicManifestEntry> Magics, IReadOnlyList<ItemManifestEntry> Items, IReadOnlyList<NpcPageManifestEntry> NpcPages, IReadOnlyList<NpcButtonManifestEntry> NpcButtons, IReadOnlyList<NpcGoodManifestEntry> NpcGoods, IReadOnlyList<QuestManifestEntry> Quests, IReadOnlyList<QuestTaskManifestEntry> QuestTasks, IReadOnlyList<MapManifestEntry> Maps);
internal sealed record ItemManifest(string Source, DateTime GeneratedUtc, IReadOnlyList<ItemManifestEntry> Items);
internal sealed record ItemManifestEntry(int Index, string Name, int ItemType, int Image, int Durability, int Price, int Weight, int StackSize, bool CanRepair, bool CanSell, bool CanStore, bool CanTrade, bool CanDrop, string Description, int Rarity);
internal sealed record NpcPageManifest(string Source, DateTime GeneratedUtc, IReadOnlyList<NpcPageManifestEntry> Pages, IReadOnlyList<NpcButtonManifestEntry> Buttons, IReadOnlyList<NpcGoodManifestEntry> Goods);
internal sealed record NpcPageManifestEntry(int Index, string Description, int DialogType, string Say, int SuccessPage, string Arguments);
internal sealed record NpcButtonManifestEntry(int Index, int PageIndex, int ButtonId, int DestinationPageIndex);
internal sealed record NpcGoodManifestEntry(int Index, int PageIndex, int ItemInfoIndex, decimal Rate);
internal sealed record QuestManifest(string Source, DateTime GeneratedUtc, IReadOnlyList<QuestManifestEntry> Quests, IReadOnlyList<QuestTaskManifestEntry> Tasks);
internal sealed record QuestManifestEntry(int Index, string Name, string AcceptText, string ProgressText, string CompletedText, string ArchiveText, int StartNpcIndex, int FinishNpcIndex);
internal sealed record QuestTaskManifestEntry(int Index, int QuestIndex, int TaskType, int ItemInfoIndex, string MobDescription, int Amount);
internal sealed record MapInfoManifest(string Source, DateTime GeneratedUtc, IReadOnlyList<MapManifestEntry> Maps);
internal sealed record MapManifestEntry(int Index, string FileName, string Description, int MiniMap);