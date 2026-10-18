using System;
using System.Text;

namespace BundleUtilities;

public class EncryptedString
{
    private ulong _encrypted;
    public ulong Encrypted
    {
        get
        {
            return _encrypted;
        }
        set
        {
            _encrypted = value;
            DecryptBytes();
        }
    }

    private string _value;
    public string Value
    {
        get
        {
            DecryptBytes(); // TEMP
            return _value;
        }
        set
        {
            _value = value;
            EncryptBytes();
        }
    }

    public EncryptedString(ulong encrypted)
    {
        Encrypted = encrypted;
    }

    public EncryptedString(string value)
    {
        Value = value;
    }

    private void DecryptBytes()
    {
        byte[] bytes = DecryptString(_encrypted);

        string val = Encoding.ASCII.GetString(bytes);
        _value = val.Trim();
    }

    private byte[] DecryptString(ulong val)
    {
        byte[] buf = new byte[12]; // Normally 13 but C# only needs 12 because \0 is automatically added
        ulong current = val;
        int index = 11;
        byte c;
        do
        {
            ulong mod = current % 0x28;
            current = current / 0x28;

            if ((byte)mod == 39)
            {
                c = (byte)'_';
            }
            else if ((sbyte)mod < 13) // signed
            {
                if ((sbyte)mod < 3) // signed
                {
                    if ((byte)mod == 2)
                    {
                        c = (byte)'/';
                    }
                    else if ((byte)mod == 1)
                    {
                        c = (byte)'-';
                    }
                    else
                    {
                        c = (byte)(mod != 0 ? 0 : ' ');
                    }
                }
                else
                {
                    c = (byte)(mod + '-');
                }
            }
            else
            {
                c = (byte)(mod + '4');
            }
            buf[--index + 1] = c;
        }
        while (index >= 0);

        return buf;
    }

    private void EncryptBytes()
    {
        ulong value = EncryptString(Encoding.ASCII.GetBytes(_value));
        _encrypted = value;
    }

    private ulong EncryptString(byte[] bytes)
    {

        unchecked
        {
            if (bytes.Length > 12)
                throw new ArgumentException("Encrypted Strings cannot be larger than 12 characters!");

            ulong current = 0;

            int index = 0;
            do
            {
                int ind = ++index - 1;
                byte c = 0;
                if (ind < bytes.Length)
                    c = bytes[ind];

                if (c > 0x00 && c < 0x20)
                    throw new ArgumentException("Encrypted Strings cannot contain characters between ASCII 0x00 and ASCII 0x20!");
                else if (c > 0x39 && c < 0x41)
                    throw new ArgumentException("Encrypted Strings cannot contain characters between ASCII 0x39 and ASCII 0x40!");
                else if (c > 0x5F) //0x5A)
                    throw new ArgumentException("Encrypted Strings cannot contain characters over ASCII 0x5F!");//0x5A!");

                byte mod = 0;
                if (c == ' ')
                    mod = 0;
                else if (c == '-')
                    mod = 1;
                else if (c == '/')
                    mod = 2;
                else if (c == '_')
                    mod = 39;
                else if (c >= '-' && c < '4')
                    mod = (byte)(c - '-');
                else if (c >= '4' && c <= '9')
                    mod = (byte)(c - '4' + 7);
                else if (c >= '4')
                    mod = (byte)(c - '4');

                current = (current * 0x28) + mod;

            } while (index < 12);
            return current;
        }
    }


    public override string ToString() => Value;
}
