using System.Collections;

if (args.Length != 1) throw new ArgumentException("Usage: Zircon.MapTransitionInspector <System.db>");
using var stream = File.OpenRead(args[0]);
using var reader = new BinaryReader(stream);
int mappingCount = Count(reader);
var mappings = new List<Mapping>();
for (int i = 0; i < mappingCount; i++)
{
    string type = reader.ReadString();
    int propertyCount = Count(reader);
    var properties = new List<Property>();
    for (int p = 0; p < propertyCount; p++) properties.Add(new(reader.ReadString(), reader.ReadString()));
    mappings.Add(new(type, properties));
}

var regions = new Dictionary<int, Region>();
var movements = new List<Movement>();
foreach (Mapping mapping in mappings)
{
    int length = reader.ReadInt32();
    byte[] data = reader.ReadBytes(length);
    if (!mapping.Type.EndsWith(".MapRegion") && !mapping.Type.EndsWith(".MovementInfo")) continue;
    Console.WriteLine($"mapping={mapping.Type} properties={string.Join(',', mapping.Properties.Select(x => x.Name + ':' + x.Type))}");
    using var dataStream = new MemoryStream(data);
    using var dataReader = new BinaryReader(dataStream);
    dataReader.ReadInt32();
    int objects = Count(dataReader);
    for (int i = 0; i < objects; i++)
    {
        int rawLength = dataReader.ReadInt32();
        using var rawStream = new MemoryStream(dataReader.ReadBytes(rawLength));
        using var rawReader = new BinaryReader(rawStream);
        var values = new Dictionary<string, object?>();
        foreach (Property property in mapping.Properties) values[property.Name] = Read(rawReader, property.Type);
        int index = Convert.ToInt32(values["Index"]);
        if (mapping.Type.EndsWith(".MapRegion"))
            regions[index] = new(index, Int(values, "Map"), Text(values, "Description"), Points(values, "PointRegion"), Int(values, "Size"));
        else
            movements.Add(new(index, Int(values, "SourceRegion"), Int(values, "DestinationRegion"), Bool(values, "NeedHole"), Int(values, "NeedItem"), Int(values, "NeedSpawn")));
    }
}

foreach (Movement movement in movements)
{
    if (!regions.TryGetValue(movement.Source, out Region? source) || !regions.TryGetValue(movement.Destination, out Region? destination)) continue;
    if (source.Map != 1 && destination.Map != 1 && source.Map != 6 && destination.Map != 6) continue;
    string sourcePoints = string.Join(';', source.Points.Take(12).Select(p => $"{p.X},{p.Y}"));
    Console.WriteLine($"movement={movement.Index} sourceMap={source.Map} sourceRegion={source.Index} sourceName={source.Name} sourcePoints={sourcePoints} sourceSize={source.Size} destinationMap={destination.Map} destinationRegion={destination.Index} destinationName={destination.Name} needHole={movement.NeedHole} needItem={movement.NeedItem} needSpawn={movement.NeedSpawn}");
}

static object? Read(BinaryReader reader, string type) => type switch
{
    "System.Boolean" => reader.ReadBoolean(), "System.Byte" => reader.ReadByte(), "System.SByte" => reader.ReadSByte(),
    "System.Int16" => reader.ReadInt16(), "System.UInt16" => reader.ReadUInt16(), "System.Int32" => reader.ReadInt32(),
    "System.UInt32" => reader.ReadUInt32(), "System.Int64" => reader.ReadInt64(), "System.UInt64" => reader.ReadUInt64(),
    "System.Single" => reader.ReadSingle(), "System.Double" => reader.ReadDouble(), "System.Decimal" => reader.ReadDecimal(),
    "System.String" => reader.ReadString(), "System.DateTime" => reader.ReadInt64(), "System.TimeSpan" => reader.ReadInt64(),
    "System.Drawing.Point" => new Point(reader.ReadInt32(), reader.ReadInt32()),
    "System.Drawing.Size" => new Point(reader.ReadInt32(), reader.ReadInt32()),
    "System.Drawing.Color" => reader.ReadInt32(),
    "System.Drawing.Point[]" => ReadPoints(reader), "System.Collections.BitArray" => ReadBits(reader),
    "System.Int32[]" => ReadInts(reader), "System.Byte[]" => reader.ReadBytes(Count(reader)),
    _ => reader.ReadInt32(),
};
static Point[] ReadPoints(BinaryReader reader)
{
    if (!reader.ReadBoolean()) return Array.Empty<Point>();
    var result = new Point[Count(reader)];
    for (int i = 0; i < result.Length; i++) result[i] = new(reader.ReadInt32(), reader.ReadInt32());
    return result;
}
static int[] ReadInts(BinaryReader reader)
{
    if (!reader.ReadBoolean()) return Array.Empty<int>();
    var result = new int[Count(reader)]; for (int i = 0; i < result.Length; i++) result[i] = reader.ReadInt32(); return result;
}
static byte[] ReadBits(BinaryReader reader) => !reader.ReadBoolean() ? Array.Empty<byte>() : reader.ReadBytes(Count(reader));
static int Count(BinaryReader reader) { int value = reader.ReadInt32(); if (value < 0 || value > 10_000_000) throw new InvalidDataException($"Invalid count {value}"); return value; }
static int Int(Dictionary<string, object?> values, string key) => values.TryGetValue(key, out object? value) && value != null ? Convert.ToInt32(value) : 0;
static bool Bool(Dictionary<string, object?> values, string key) => values.TryGetValue(key, out object? value) && value != null && Convert.ToBoolean(value);
static string Text(Dictionary<string, object?> values, string key) => values.TryGetValue(key, out object? value) ? value?.ToString() ?? "" : "";
static Point[] Points(Dictionary<string, object?> values, string key) => values.TryGetValue(key, out object? value) && value is Point[] points ? points : Array.Empty<Point>();
internal sealed record Mapping(string Type, IReadOnlyList<Property> Properties);
internal sealed record Property(string Name, string Type);
internal sealed record Region(int Index, int Map, string Name, Point[] Points, int Size);
internal sealed record Movement(int Index, int Source, int Destination, bool NeedHole, int NeedItem, int NeedSpawn);
internal readonly record struct Point(int X, int Y);
