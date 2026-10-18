namespace BundleUtilities;

public static class Lookup8
{
    public static ulong Hash(byte[] key, ulong length, ulong level)
    {
        ulong a, b, c, len; 

        /* Set up the internal state */
        len = length;
        a = b = level;                         /* the previous hash value */
        c = 0x9e3779b97f4a7c13L; /* the golden ratio; an arbitrary value, CHECK REMOVED THE LAST L */
        int index = 0;
        /*---------------------------------------- handle most of the key */
        while (len >= 24)
        {
            a += (key[index + 0] + ((ulong)key[index + 1] << 8) + ((ulong)key[index + 2] << 16) + ((ulong)key[index + 3] << 24)
             + ((ulong)key[index + 4] << 32) + ((ulong)key[index + 5] << 40) + ((ulong)key[index + 6] << 48) + ((ulong)key[index + 7] << 56));
            b += (key[index + 8] + ((ulong)key[index + 9] << 8) + ((ulong)key[index + 10] << 16) + ((ulong)key[index + 11] << 24)
             + ((ulong)key[index + 12] << 32) + ((ulong)key[index + 13] << 40) + ((ulong)key[index + 14] << 48) + ((ulong)key[index + 15] << 56));
            c += (key[index + 16] + ((ulong)key[index + 17] << 8) + ((ulong)key[index + 18] << 16) + ((ulong)key[index + 19] << 24)
             + ((ulong)key[index + 20] << 32) + ((ulong)key[index + 21] << 40) + ((ulong)key[index + 22] << 48) + ((ulong)key[index + 23] << 56));
            Mix64(ref a, ref b, ref c);
            index += 24; len -= 24;
        }

        /*------------------------------------- handle the last 23 bytes */
        c += length;
        switch (len)              /* all the case statements fall through */
        {
            case 23: c += ((ulong)key[index + 22] << 56); goto case 22;
            case 22: c += ((ulong)key[index + 21] << 48); goto case 21;
            case 21: c += ((ulong)key[index + 20] << 40); goto case 20;
            case 20: c += ((ulong)key[index + 19] << 32); goto case 19;
            case 19: c += ((ulong)key[index + 18] << 24); goto case 18;
            case 18: c += ((ulong)key[index + 17] << 16); goto case 17;
            case 17: c += ((ulong)key[index + 16] << 8); goto case 16;
            /* the first byte of c is reserved for the length */
            case 16: b += ((ulong)key[index + 15] << 56); goto case 15;
            case 15: b += ((ulong)key[index + 14] << 48); goto case 14;
            case 14: b += ((ulong)key[index + 13] << 40); goto case 13;
            case 13: b += ((ulong)key[index + 12] << 32); goto case 12;
            case 12: b += ((ulong)key[index + 11] << 24); goto case 11;
            case 11: b += ((ulong)key[index + 10] << 16); goto case 10;
            case 10: b += ((ulong)key[index + 9] << 8); goto case 9;
            case 9: b += ((ulong)key[index + 8]); goto case 8;
            case 8: a += ((ulong)key[index + 7] << 56); goto case 7;
            case 7: a += ((ulong)key[index + 6] << 48); goto case 6;
            case 6: a += ((ulong)key[index + 5] << 40); goto case 5;
            case 5: a += ((ulong)key[index + 4] << 32); goto case 4;
            case 4: a += ((ulong)key[index + 3] << 24); goto case 3;
            case 3: a += ((ulong)key[index + 2] << 16); goto case 2;
            case 2: a += ((ulong)key[index + 1] << 8); goto case 1;
            case 1: a += ((ulong)key[index + 0]); break; 
                /* case 0: nothing left to add */
        }
        Mix64(ref a, ref b, ref c);
        /*-------------------------------------------- report the result */
        return c;
    }
    // Not sure if that works
    public static void Mix64(ref ulong a, ref ulong b, ref ulong c)
    {
        a -= b; a -= c; a ^= (c >> 43);
        b -= c; b -= a; b ^= (a << 9);
        c -= a; c -= b; c ^= (b >> 8);
        a -= b; a -= c; a ^= (c >> 38);
        b -= c; b -= a; b ^= (a << 23);
        c -= a; c -= b; c ^= (b >> 5);
        a -= b; a -= c; a ^= (c >> 35);
        b -= c; b -= a; b ^= (a << 49);
        c -= a; c -= b; c ^= (b >> 11);
        a -= b; a -= c; a ^= (c >> 12);
        b -= c; b -= a; b ^= (a << 18);
        c -= a; c -= b; c ^= (b >> 22);
    }

}
