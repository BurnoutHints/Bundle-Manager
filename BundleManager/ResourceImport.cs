using BundleFormat;
using BurnoutImage;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BundleManager
{
    public record PrimaryResource(byte[] Data, byte[] ImageBody, List<Dependency> Imports, string ImportsSource, EntryType Type);

    public static partial class ResourceImport
    {
        private static readonly string[] ImageExtensions = [".dds", ".bmp", ".gif", ".jpg", ".jpeg", ".png", ".tif", ".tiff", ".tga", ".webp"];

        public static PrimaryResource LoadPrimary(string path, BundleArchive archive, Func<PlatformType> choosePCTexturePlatform)
        {
            if (ImageExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            {
                PlatformType platform = GetImageTexturePlatform(archive, choosePCTexturePlatform)
                    ?? throw new NotSupportedException($"Images can't be converted for {archive.Platform} bundles. Import the texture's header and body instead.");

                ImageInfo info = GameImage.SetImage(path, CompressionType.DXT5, platform);
                return new(info.Header, info.Data, [], null, EntryType.Texture);
            }

            byte[] data = ReadPrimary(path, archive.Console, out List<Dependency> imports);
            string importsSource = imports != null ? Path.GetFileName(GetImportsYamlPath(path)) : null;
            imports ??= ScanImports(data, archive.Platform);

            EntryType type = InferEntryType(data[..^(imports.Count * Dependency.Size)], imports.Count, archive.Console);
            return new(data, null, imports, importsSource, type);
        }

        public static byte[] ReadPrimary(string path, bool bigEndian, out List<Dependency> imports)
        {
            byte[] data = File.ReadAllBytes(path);
            string yamlPath = GetImportsYamlPath(path);
            imports = yamlPath != null && File.Exists(yamlPath) ? LoadDependenciesFromYaml(yamlPath) : null;
            return imports != null ? Dependency.AppendTable(data, imports, bigEndian) : data;
        }

        // We now export the primary data without its import table and include a YAP-style yaml file for imports
        public static void WritePrimary(string path, BundleEntry entry)
        {
            byte[] data = entry.EntryBlocks[0].Data;
            List<BundleDependency> imports = entry.GetDependencies();

            int length = imports.Count > 0 && entry.Dependencies.Count == 0 ? entry.DependenciesListOffset : data.Length;
            File.WriteAllBytes(path, data[..length]);

            string yamlPath = GetImportsYamlPath(path);
            if (imports.Count > 0)
                File.WriteAllLines(yamlPath, imports.Select(d => $"- 0x{d.EntryPointerOffset:x8}: 0x{d.EntryID:x8}"));
            else if (File.Exists(yamlPath))
                File.Delete(yamlPath); // From an earlier export of this resource, it would add imports back on import
        }

        public static List<Dependency> ScanImports(byte[] data, BundlePlatform platform)
        {
            bool bigEndian = platform is BundlePlatform.X360 or BundlePlatform.PS3;
            return Dependency.ScanTable(data, bigEndian, platform == BundlePlatform.PCx64 ? 8 : 4);
        }

        public static string GetResourceBaseName(string path)
        {
            const string primarySuffix = "_primary";
            string name = Path.GetFileNameWithoutExtension(path);

            return name.EndsWith(primarySuffix, StringComparison.OrdinalIgnoreCase)
                ? name[..^primarySuffix.Length]
                : name;
        }

        public static string FindSecondary(string primaryPath)
        {
            string secondary = Path.Combine(Path.GetDirectoryName(primaryPath), GetResourceBaseName(primaryPath) + "_secondary" + Path.GetExtension(primaryPath));
            return File.Exists(secondary) ? secondary : null;
        }

        // Exports are named after the resource ID, e.g. 0A7B302C_primary.dat
        public static uint? GetIDFromFileName(string path)
        {
            Match match = ResourceIDPattern().Match(Path.GetFileNameWithoutExtension(path));
            return match.Success ? uint.Parse(match.Value, NumberStyles.HexNumber) : null;
        }

        public static bool TryParseHex(string s, out uint value)
        {
            return uint.TryParse(s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? s[2..] : s,
                NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        }

        public static bool TextureMatchesBundle(byte[] header, BundlePlatform bundle)
        {
            PlatformType texture = GameImage.DetectPlatform(header);
            return texture == TexturePlatform(bundle) || (texture == PlatformType.Remastered && bundle == BundlePlatform.PC);
        }

        private static PlatformType? TexturePlatform(BundlePlatform platform) => platform switch
        {
            BundlePlatform.PC => PlatformType.PC,
            BundlePlatform.PCx64 => PlatformType.PCx64,
            BundlePlatform.X360 => PlatformType.X360,
            BundlePlatform.PS3 => PlatformType.PS3,
            _ => null
        };

        private static PlatformType? GetImageTexturePlatform(BundleArchive archive, Func<PlatformType> choosePCTexturePlatform)
        {
            BundleEntry sample = archive.Entries.FirstOrDefault(e => e.Type == EntryType.Texture && e.HasHeader);
            PlatformType? platform = sample != null
                ? GameImage.DetectPlatform(sample.EntryBlocks[0].Data)
                : archive.Platform == BundlePlatform.PC
                    ? choosePCTexturePlatform()
                    : TexturePlatform(archive.Platform);

            // GameImage.SetImage can only write these
            return platform is PlatformType.PC or PlatformType.PCx64 or PlatformType.Remastered ? platform : null;
        }

        private static EntryType InferEntryType(byte[] data, int importCount, bool bigEndian)
        {
            bool Has(ReadOnlySpan<byte> magic) => data.AsSpan().IndexOf(magic) >= 0;

            // AttribSysVault
            if (bigEndian
                ? Has("StrN"u8) && Has("Vers"u8)
                : Has("NrtS"u8) && Has("sreV"u8))
            {
                return EntryType.AttribSysVault;
            }

            EntryType type = EntryType.Invalid;

            // Texture
            if (GameImage.DetectPlatform(data) is PlatformType p
                && p != PlatformType.Invalid
                && importCount == 0)
            {
                type = EntryType.Texture;
            }
            // Model
            else if (data.Length <= 0x20)
            {
                // 32 bit model, then 64 bit model (32 bit is technically padded to nearest 0x10, so this would run anyway)
                if ((data.Length > 0x13 && data[0x13] == 0x2)
                    || (data.Length > 0x1F && data[0x1F] == 0x2))
                    type = EntryType.Model;
            }
            // Renderable
            else
            {
                ReadOnlySpan<byte> version = data.AsSpan(0x10); // mu16VersionNumber
                if ((bigEndian ? BinaryPrimitives.ReadUInt16BigEndian(version) : BinaryPrimitives.ReadUInt16LittleEndian(version)) == 11)
                    type = EntryType.Renderable;
            }

            return type;
        }

        private static string GetImportsYamlPath(string primaryPath)
        {
            string dir = Path.GetDirectoryName(primaryPath);
            string name = GetResourceBaseName(primaryPath);

            if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(name))
                return null;

            return Path.Combine(dir, name + "_imports.yaml");
        }

        private static List<Dependency> LoadDependenciesFromYaml(string yamlPath)
        {
            List<Dependency> list = [];

            foreach (string line in File.ReadLines(yamlPath))
            {
                string[] parts = line.Trim().TrimStart('-').Split(':');
                if (parts.Length == 2 && TryParseHex(parts[0].Trim(), out uint offset) && TryParseHex(parts[1].Trim(), out uint id))
                    list.Add(new Dependency { EntryPointerOffset = offset, ID = id });
            }

            return list;
        }

        [GeneratedRegex(@"^[0-9A-Fa-f]{8}(?![0-9A-Fa-f])")]
        private static partial Regex ResourceIDPattern();
    }
}
