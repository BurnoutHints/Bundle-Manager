using System.ComponentModel;
using BaseHandlers;
using BundleFormat;
using static BaseHandlers.HudMessages;

internal static partial class Program
{
    // The game's tools leave junk in the padding after macMessageId and at the end of each record
    private static byte[] WithoutPadding(byte[] data, HudMessages messages)
    {
        byte[] copy = [.. data];
        int slots = messages.StringSlots, record = messages.RecordSize, id = slots * 64 + 64 + 13, hash = (id + 7) / 8 * 8, end = hash + 32 + slots * 20;
        int first = messages.OnePerResource
            ? 0
            : data.Length - messages.Messages.Count * record;

        for (int start = first; start < data.Length; start += record)
        {
            Array.Clear(copy, start + id, hash - id);
            Array.Clear(copy, start + end, record - end);
        }

        return copy;
    }

    private static void CheckHudMessageEditor(HudMessages source, BundleEntry entry, string label)
    {
        byte[] before = [.. entry.EntryBlocks[0].Data];
        using HudMessagesEditor editor = new(source, entry);
        Hidden(editor);
        HudMessages draft = Field<HudMessages>(editor, "data");
        ObjectTreeEditor tree = Field<ObjectTreeEditor>(editor, "tree");
        HudMessage message = draft.Messages[0];
        PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(message);
        Check(properties["macMessageId"].DisplayName == "Message ID" && properties["mMessageIdHash"].IsReadOnly && properties["Online"].Category == "Availability"
            && properties["meMessageGroup"].Converter.ConvertToString(HudMessageGroup.E_HUDMESSAGEGROUP_ONLINE_DIRTY_TRICKS) == "Online Dirty Tricks",
            $"{label}: message fields have readable names, a read-only hash and availability switches");
        Check(tree.CollectionLimit(message, nameof(HudMessage.Strings)) == draft.StringSlots, $"{label}: a message can hold {draft.StringSlots} strings");

        tree.SelectObject(message);
        editor.ClientSize = new(1000, 700);
        Application.DoEvents();
        Capture(editor, $"hud-{entry.ID:X8}-{draft.StringSlots}.png");

        properties["Online"].SetValue(message, !message.Online);
        properties["mfDuration"].SetValue(message, 7f);
        tree.RecordChange();
        Check(entry.EntryBlocks[0].Data.SequenceEqual(before) && (bool)Call(editor, "ApplyChanges"), $"{label}: edits stay in the draft until Apply writes them");
        HudMessages applied = new();
        applied.Read(entry);
        Check(applied.Messages[0].mfDuration == 7f && applied.Messages[0].Online == message.Online && source.Messages[0].mfDuration != 7f, $"{label}: Apply writes the edited message to the resource");

        message.macMessageId = "";
        tree.RecordChange();
        Check(Field<ListBox>(editor, "issues").Items.Count > 0 && !(bool)Call(editor, "ApplyChanges"), $"{label}: a message without an ID is listed and blocks Apply");
        entry.EntryBlocks[0].Data = before;
    }

    private static void CheckHudMessageFiles(string[] args)
    {
        foreach (string argument in args.Where(argument => argument.StartsWith("--hud-file=")))
        {
            string path = argument["--hud-file=".Length..];
            BundleArchive bundle = BundleArchive.Read(path);
            string label = $"{Path.GetFileName(Path.GetDirectoryName(path))}/{Path.GetFileName(path)}";
            BundleEntry[] entries = [.. bundle.Entries.Where(e => e.Type == EntryType.HudMessage)];
            int same = 0;

            foreach (BundleEntry entry in entries)
            {
                byte[] original = entry.EntryBlocks[0].Data;
                HudMessages data = new();
                data.Read(entry);
                Check(!data.Problems().Any(), $"{label} {entry.ID:X8}: {data.Messages.Count} messages with {data.StringSlots} string slots have no issues");

                byte[] saved = data.Serialize(entry);
                Equal(WithoutPadding(saved, data), WithoutPadding(original, data), $"{label} {entry.ID:X8}: saves the same bytes apart from padding");
                same += saved.AsSpan().SequenceEqual(original) ? 1 : 0;
            }

            if (entries.Length == 0)
                continue;

            BundleEntry first = entries[0];
            HudMessages opened = new();
            opened.Read(first);
            CheckHudMessageEditor(opened, first, label);
            HudMessages edited = new();
            edited.Read(first);
            HudMessage message = edited.Messages[0];
            message.macMessageId = "TestMsg";
            message.Online = !message.Online;
            message.mfDuration = 9.5f;

            if (message.Strings.Count < edited.StringSlots)
                message.Strings.Add(new()
                {
                    macStringId = "HUDMESSAGE_TEST",
                    Param1 = HudMessageParamTypes.E_HUDMESSAGEPARAMTYPES_INT,
                    Param2 = HudMessageParamTypes.E_HUDMESSAGEPARAMTYPES_MONEY
                });

            Check(edited.Write(first), $"{label}: an edited message saves");
            HudMessages reread = new();
            reread.Read(first);
            HudMessage back = reread.Messages[0];
            Check(back.macMessageId == "TestMsg" && back.mMessageIdHash == new BundleUtilities.EncryptedString("TESTMSG").Encrypted
                && back.Online == message.Online && back.mfDuration == 9.5f && back.Strings.Count == message.Strings.Count
                && back.Strings[^1].macStringId == message.Strings[^1].macStringId && back.Strings[^1].Param2 == message.Strings[^1].Param2,
                $"{label}: an edited message reads back with its new ID, hash, flags, timing and strings");

            if (!edited.OnePerResource)
            {
                edited.Messages.Add((HudMessage)ObjectTreeEditor.Clone(edited.Messages[^1]));
                Check(edited.Problems().Any(p => p.Message.Contains("only find the first")), $"{label}: a duplicate message ID is reported");
                edited.Messages[^1].macMessageId = "TestMsg2";
                edited.Write(first);
                reread.Read(first);
                Check(reread.Messages.Count == edited.Messages.Count && reread.Messages[^1].macMessageId == "TestMsg2", $"{label}: an added message reads back");
            }

            message.Strings[0].Param1 = HudMessageParamTypes.E_HUDMESSAGEPARAMTYPES_UNUSED;
            message.Strings[0].Param2 = HudMessageParamTypes.E_HUDMESSAGEPARAMTYPES_INT;
            Check(edited.Problems().Any(p => p.Message.Contains("after an unused one")), $"{label}: a parameter after an unused one is reported");
            Console.WriteLine($"{label}: {same} of {entries.Length} resources were byte for byte identical");
        }
    }
}
