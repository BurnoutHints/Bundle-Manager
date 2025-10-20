using System.Text;

namespace BundleFormat.Types;

public class CgsId
{
	internal ulong id;
	internal string Id
	{
		get => Uncompress(id);
		set => id = Compress(value);
	}

	internal static string Uncompress(ulong? compressedId)
	{
		if (compressedId == null || compressedId == 0)
			return "";

		StringBuilder id = new();

		for (int i = 0; i < 12; i++)
		{
			ulong mod = (ulong)compressedId % 40;

			if (mod == 39)
				id.Insert(0, '_');
			else if (mod >= 13)
				id.Insert(0, (char)(mod + 52));
			else if (mod >= 3)
				id.Insert(0, (char)(mod + 45));
			else if (mod >= 2)
				id.Insert(0, '/');
			else
			{
				mod = (mod - 1) & 32;
				id.Insert(0, (char)mod);
			}

			compressedId /= 40;
		}

		return id.ToString().TrimEnd();
	}

	internal static ulong Compress(string? id)
	{
		if (id == null || id.Length < 1)
			return 0;
		else if (id.Length > 12)
			throw new Exception("CgsId length cannot be greater than 12.");

		ulong compressedId = 0;
		const ulong multiplier = 40;
		for (int i = 0; i < 12; ++i)
		{
			sbyte character = 0;
			if (i < id.Length)
				character = (sbyte)id[i];
			if (character == 0)
				character = 32;

			if (!(character == 32 || character == 45 || character == 47 || (character >= 48 && character <= 57)
				|| (character >= 65 && character <= 90) || character == 95))
				continue;

			ulong addend = 0;
			if (character == 95)
				addend = 39;
			else if (character >= 65)
				addend = (ulong)(character - 52);
			else if (character >= 48)
				addend = (ulong)(character - 45);
			else if (character >= 47)
				addend = 2;
			else if (character >= 45)
				addend = 1;
			compressedId = compressedId * multiplier + addend;
		}

		return compressedId;
	}
}
