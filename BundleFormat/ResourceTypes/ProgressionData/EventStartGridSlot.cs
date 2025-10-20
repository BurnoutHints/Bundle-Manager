using Serialization.Attributes;

namespace BundleFormat.ResourceTypes.ProgressionData;

[AlignEnd]
internal class EventStartGridSlot
{
	[Flags]
	internal enum Flags : byte
	{
		CanDeviateFromRoute = 1,
		CanTakeShortcuts = 1 << 1
	}

	internal uint opponentIndex;
	internal uint personalityIndex;
	internal int fastAiBalanceGraphIndex;
	internal int slowAiBalanceGraphIndex;
	internal byte colourIndex;
	internal byte flags;

	[Bounds(0, 6)]
	public uint OpponentIndex { get => opponentIndex; set => opponentIndex = value; }
	[FromArray(nameof(BPR.ProgressionData.Personalities))]
	public int PersonalityIndex { get => (int)personalityIndex; set => personalityIndex = (uint)value; }
	[FromArray(nameof(BPR.ProgressionData.AiBalances))]
	public int FastAiBalanceGraphIndex { get => fastAiBalanceGraphIndex; set => fastAiBalanceGraphIndex = value; }
	[FromArray(nameof(BPR.ProgressionData.AiBalances))]
	public int SlowAiBalanceGraphIndex { get => slowAiBalanceGraphIndex; set => slowAiBalanceGraphIndex = value; }
	public byte ColourIndex { get => colourIndex; set => colourIndex = value; }
	public bool CanDeviateFromRoute
	{
		get => (flags & (byte)Flags.CanDeviateFromRoute) != 0;
		set
		{
			if (value)
				flags |= (byte)Flags.CanDeviateFromRoute;
			else
				flags &= (byte)~Flags.CanDeviateFromRoute;
		}
	}
	public bool CanTakeShortcuts
	{
		get => (flags & (byte)Flags.CanTakeShortcuts) != 0;
		set
		{
			if (value)
				flags |= (byte)Flags.CanTakeShortcuts;
			else
				flags &= (byte)~Flags.CanTakeShortcuts;
		}
	}
}
