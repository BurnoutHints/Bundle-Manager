using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace BundleFormat;

// Known resource names by ID from ResourceDB.json
public static class ResourceDB
{
    private static readonly Lazy<(byte[] Json, Dictionary<uint, int> Offsets)> _db = new(Load);

    // Indexes the file in the background, so the first lookup doesn't wait for it
    public static void Preload() => Task.Run(() => _db.Value);

    public static string Lookup(ulong id)
    {
        if (id > uint.MaxValue || !_db.Value.Offsets.TryGetValue((uint)id, out int offset))
            return null;

        Utf8JsonReader reader = new(_db.Value.Json.AsSpan(offset));
        reader.Read();
        return reader.GetString();
    }

    private static (byte[], Dictionary<uint, int>) Load()
    {
        byte[] json = [];
        Dictionary<uint, int> offsets = [];
        try
        {
            json = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "ResourceDB.json"));
            offsets.EnsureCapacity(json.Length / 64); // Entries average ~80 bytes

            Utf8JsonReader reader = new(json);
            while (reader.Read())
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                bool isID = Utf8Parser.TryParse(reader.ValueSpan, out uint id, out int length, 'x') && length == reader.ValueSpan.Length;
                if (reader.Read() && isID && reader.TokenType == JsonTokenType.String)
                    offsets.TryAdd(id, (int)reader.TokenStartIndex);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            // Missing or broken file
        }

        return (json, offsets);
    }
}
