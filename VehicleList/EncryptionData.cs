namespace VehicleList;

public static class EncryptionData
{
    private static uint[] data;

    public static uint[] Data
    {
        get
        {
            if (data == null)
                InitData();

            return data;
        }
    }

    private static void InitData()
    {
        data = new uint[256];

        data[000] = 0x00000000;
        data[001] = 0x77073096;
        data[002] = 0xEE0E612C;
        data[003] = 0x990951BA;
        data[004] = 0x076DC419;
        data[005] = 0x706AF48F;
        data[006] = 0xE963A535;
        data[007] = 0x9E6495A3;
        data[008] = 0x0EDB8832;
        data[009] = 0x79DCB8A4;
        data[010] = 0xE0D5E91E;
        data[011] = 0x97D2D988;
        data[012] = 0x09B64C2B;
        data[013] = 0x7EB17CBD;
        data[014] = 0xE7B82D07;
        data[015] = 0x90BF1D91;
        data[016] = 0x1DB71064;
        data[017] = 0x6AB020F2;
        data[018] = 0xF3B97148;
        data[019] = 0x84BE41DE;
        data[020] = 0x1ADAD47D;
        data[021] = 0x6DDDE4EB;
        data[022] = 0xF4D4B551;
        data[023] = 0x83D385C7;
        data[024] = 0x136C9856;
        data[025] = 0x646BA8C0;
        data[026] = 0xFD62F97A;
        data[027] = 0x8A65C9EC;
        data[028] = 0x14015C4F;
        data[029] = 0x63066CD9;
        data[030] = 0xFA0F3D63;
        data[031] = 0x8D080DF5;
        data[032] = 0x3B6E20C8;
        data[033] = 0x4C69105E;
        data[034] = 0xD56041E4;
        data[035] = 0xA2677172;
        data[036] = 0x3C03E4D1;
        data[037] = 0x4B04D447;
        data[038] = 0xD20D85FD;
        data[039] = 0xA50AB56B;
        data[040] = 0x35B5A8FA;
        data[041] = 0x42B2986C;
        data[042] = 0xDBBBC9D6;
        data[043] = 0xACBCF940;
        data[044] = 0x32D86CE3;
        data[045] = 0x45DF5C75;
        data[046] = 0xDCD60DCF;
        data[047] = 0xABD13D59;
        data[048] = 0x26D930AC;
        data[049] = 0x51DE003A;
        data[050] = 0xC8D75180;
        data[051] = 0xBFD06116;
        data[052] = 0x21B4F4B5;
        data[053] = 0x56B3C423;
        data[054] = 0xCFBA9599;
        data[055] = 0xB8BDA50F;
        data[056] = 0x2802B89E;
        data[057] = 0x5F058808;
        data[058] = 0xC60CD9B2;
        data[059] = 0xB10BE924;
        data[060] = 0x2F6F7C87;
        data[061] = 0x58684C11;
        data[062] = 0xC1611DAB;
        data[063] = 0xB6662D3D;
        data[064] = 0x76DC4190;
        data[065] = 0x01DB7106;
        data[066] = 0x98D220BC;
        data[067] = 0xEFD5102A;
        data[068] = 0x71B18589;
        data[069] = 0x06B6B51F;
        data[070] = 0x9FBFE4A5;
        data[071] = 0xE8B8D433;
        data[072] = 0x7807C9A2;
        data[073] = 0x0F00F934;
        data[074] = 0x9609A88E;
        data[075] = 0xE10E9818;
        data[076] = 0x7F6A0DBB;
        data[077] = 0x086D3D2D;
        data[078] = 0x91646C97;
        data[079] = 0xE6635C01;
        data[080] = 0x6B6B51F4;
        data[081] = 0x1C6C6162;
        data[082] = 0x856530D8;
        data[083] = 0xF262004E;
        data[084] = 0x6C0695ED;
        data[085] = 0x1B01A57B;
        data[086] = 0x8208F4C1;
        data[087] = 0xF50FC457;
        data[088] = 0x65B0D9C6;
        data[089] = 0x12B7E950;
        data[090] = 0x8BBEB8EA;
        data[091] = 0xFCB9887C;
        data[092] = 0x62DD1DDF;
        data[093] = 0x15DA2D49;
        data[094] = 0x8CD37CF3;
        data[095] = 0xFBD44C65;
        data[096] = 0x4DB26158;
        data[097] = 0x3AB551CE;
        data[098] = 0xA3BC0074;
        data[099] = 0xD4BB30E2;
        data[100] = 0x4ADFA541;
        data[101] = 0x3DD895D7;
        data[102] = 0xA4D1C46D;
        data[103] = 0xD3D6F4FB;
        data[104] = 0x4369E96A;
        data[105] = 0x346ED9FC;
        data[106] = 0xAD678846;
        data[107] = 0xDA60B8D0;
        data[108] = 0x44042D73;
        data[109] = 0x33031DE5;
        data[110] = 0xAA0A4C5F;
        data[111] = 0xDD0D7CC9;
        data[112] = 0x5005713C;
        data[113] = 0x270241AA;
        data[114] = 0xBE0B1010;
        data[115] = 0xC90C2086;
        data[116] = 0x5768B525;
        data[117] = 0x206F85B3;
        data[118] = 0xB966D409;
        data[119] = 0xCE61E49F;
        data[120] = 0x5EDEF90E;
        data[121] = 0x29D9C998;
        data[122] = 0xB0D09822;
        data[123] = 0xC7D7A8B4;
        data[124] = 0x59B33D17;
        data[125] = 0x2EB40D81;
        data[126] = 0xB7BD5C3B;
        data[127] = 0xC0BA6CAD;
        data[128] = 0xEDB88320;
        data[129] = 0x9ABFB3B6;
        data[130] = 0x03B6E20C;
        data[131] = 0x74B1D29A;
        data[132] = 0xEAD54739;
        data[133] = 0x9DD277AF;
        data[134] = 0x04DB2615;
        data[135] = 0x73DC1683;
        data[136] = 0xE3630B12;
        data[137] = 0x94643B84;
        data[138] = 0x0D6D6A3E;
        data[139] = 0x7A6A5AA8;
        data[140] = 0xE40ECF0B;
        data[141] = 0x9309FF9D;
        data[142] = 0x0A00AE27;
        data[143] = 0x7D079EB1;
        data[144] = 0xF00F9344;
        data[145] = 0x8708A3D2;
        data[146] = 0x1E01F268;
        data[147] = 0x6906C2FE;
        data[148] = 0xF762575D;
        data[149] = 0x806567CB;
        data[150] = 0x196C3671;
        data[151] = 0x6E6B06E7;
        data[152] = 0xFED41B76;
        data[153] = 0x89D32BE0;
        data[154] = 0x10DA7A5A;
        data[155] = 0x67DD4ACC;
        data[156] = 0xF9B9DF6F;
        data[157] = 0x8EBEEFF9;
        data[158] = 0x17B7BE43;
        data[159] = 0x60B08ED5;
        data[160] = 0xD6D6A3E8;
        data[161] = 0xA1D1937E;
        data[162] = 0x38D8C2C4;
        data[163] = 0x4FDFF252;
        data[164] = 0xD1BB67F1;
        data[165] = 0xA6BC5767;
        data[166] = 0x3FB506DD;
        data[167] = 0x48B2364B;
        data[168] = 0xD80D2BDA;
        data[169] = 0xAF0A1B4C;
        data[170] = 0x36034AF6;
        data[171] = 0x41047A60;
        data[172] = 0xDF60EFC3;
        data[173] = 0xA867DF55;
        data[174] = 0x316E8EEF;
        data[175] = 0x4669BE79;
        data[176] = 0xCB61B38C;
        data[177] = 0xBC66831A;
        data[178] = 0x256FD2A0;
        data[179] = 0x5268E236;
        data[180] = 0xCC0C7795;
        data[181] = 0xBB0B4703;
        data[182] = 0x220216B9;
        data[183] = 0x5505262F;
        data[184] = 0xC5BA3BBE;
        data[185] = 0xB2BD0B28;
        data[186] = 0x2BB45A92;
        data[187] = 0x5CB36A04;
        data[188] = 0xC2D7FFA7;
        data[189] = 0xB5D0CF31;
        data[190] = 0x2CD99E8B;
        data[191] = 0x5BDEAE1D;
        data[192] = 0x9B64C2B0;
        data[193] = 0xEC63F226;
        data[194] = 0x756AA39C;
        data[195] = 0x026D930A;
        data[196] = 0x9C0906A9;
        data[197] = 0xEB0E363F;
        data[198] = 0x72076785;
        data[199] = 0x05005713;
        data[200] = 0x95BF4A82;
        data[201] = 0xE2B87A14;
        data[202] = 0x7BB12BAE;
        data[203] = 0x0CB61B38;
        data[204] = 0x92D28E9B;
        data[205] = 0xE5D5BE0D;
        data[206] = 0x7CDCEFB7;
        data[207] = 0x0BDBDF21;
        data[208] = 0x86D3D2D4;
        data[209] = 0xF1D4E242;
        data[210] = 0x68DDB3F8;
        data[211] = 0x1FDA836E;
        data[212] = 0x81BE16CD;
        data[213] = 0xF6B9265B;
        data[214] = 0x6FB077E1;
        data[215] = 0x18B74777;
        data[216] = 0x88085AE6;
        data[217] = 0xFF0F6A70;
        data[218] = 0x66063BCA;
        data[219] = 0x11010B5C;
        data[220] = 0x8F659EFF;
        data[221] = 0xF862AE69;
        data[222] = 0x616BFFD3;
        data[223] = 0x166CCF45;
        data[224] = 0xA00AE278;
        data[225] = 0xD70DD2EE;
        data[226] = 0x4E048354;
        data[227] = 0x3903B3C2;
        data[228] = 0xA7672661;
        data[229] = 0xD06016F7;
        data[230] = 0x4969474D;
        data[231] = 0x3E6E77DB;
        data[232] = 0xAED16A4A;
        data[233] = 0xD9D65ADC;
        data[234] = 0x40DF0B66;
        data[235] = 0x37D83BF0;
        data[236] = 0xA9BCAE53;
        data[237] = 0xDEBB9EC5;
        data[238] = 0x47B2CF7F;
        data[239] = 0x30B5FFE9;
        data[240] = 0xBDBDF21C;
        data[241] = 0xCABAC28A;
        data[242] = 0x53B39330;
        data[243] = 0x24B4A3A6;
        data[244] = 0xBAD03605;
        data[245] = 0xCDD70693;
        data[246] = 0x54DE5729;
        data[247] = 0x23D967BF;
        data[248] = 0xB3667A2E;
        data[249] = 0xC4614AB8;
        data[250] = 0x5D681B02;
        data[251] = 0x2A6F2B94;
        data[252] = 0xB40BBE37;
        data[253] = 0xC30C8EA1;
        data[254] = 0x5A05DF1B;
        data[255] = 0x2D02EF8D;
    }

    public static uint EncryptString(string str)
    {
        unchecked
        {
            uint v4 = (uint)-1;

            for (int i = 0; i < str.Length; i++)
            {
                byte c = (byte)str[i];

                if ((byte)(c - 65) <= 0x19)
                    c += 32;

                int v5 = c ^ (byte)v4;
                v4 = Data[v5] ^ (v4 >> 8);
            }

            v4 = ~v4;

            return v4;
        }
    }
}
