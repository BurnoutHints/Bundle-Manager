namespace VehicleList
{
    public static class EncryptionData
    {
        private static uint[] _data;
        public static uint[] Data
        {
            get
            {
                if (_data == null)
                    InitData();
                return _data;
            }
        }
        private static void InitData()
        {
            _data = new uint[256];

            _data[000] = 0x00000000;
            _data[001] = 0x77073096;
            _data[002] = 0xEE0E612C;
            _data[003] = 0x990951BA;
            _data[004] = 0x076DC419;
            _data[005] = 0x706AF48F;
            _data[006] = 0xE963A535;
            _data[007] = 0x9E6495A3;
            _data[008] = 0x0EDB8832;
            _data[009] = 0x79DCB8A4;
            _data[010] = 0xE0D5E91E;
            _data[011] = 0x97D2D988;
            _data[012] = 0x09B64C2B;
            _data[013] = 0x7EB17CBD;
            _data[014] = 0xE7B82D07;
            _data[015] = 0x90BF1D91;
            _data[016] = 0x1DB71064;
            _data[017] = 0x6AB020F2;
            _data[018] = 0xF3B97148;
            _data[019] = 0x84BE41DE;
            _data[020] = 0x1ADAD47D;
            _data[021] = 0x6DDDE4EB;
            _data[022] = 0xF4D4B551;
            _data[023] = 0x83D385C7;
            _data[024] = 0x136C9856;
            _data[025] = 0x646BA8C0;
            _data[026] = 0xFD62F97A;
            _data[027] = 0x8A65C9EC;
            _data[028] = 0x14015C4F;
            _data[029] = 0x63066CD9;
            _data[030] = 0xFA0F3D63;
            _data[031] = 0x8D080DF5;
            _data[032] = 0x3B6E20C8;
            _data[033] = 0x4C69105E;
            _data[034] = 0xD56041E4;
            _data[035] = 0xA2677172;
            _data[036] = 0x3C03E4D1;
            _data[037] = 0x4B04D447;
            _data[038] = 0xD20D85FD;
            _data[039] = 0xA50AB56B;
            _data[040] = 0x35B5A8FA;
            _data[041] = 0x42B2986C;
            _data[042] = 0xDBBBC9D6;
            _data[043] = 0xACBCF940;
            _data[044] = 0x32D86CE3;
            _data[045] = 0x45DF5C75;
            _data[046] = 0xDCD60DCF;
            _data[047] = 0xABD13D59;
            _data[048] = 0x26D930AC;
            _data[049] = 0x51DE003A;
            _data[050] = 0xC8D75180;
            _data[051] = 0xBFD06116;
            _data[052] = 0x21B4F4B5;
            _data[053] = 0x56B3C423;
            _data[054] = 0xCFBA9599;
            _data[055] = 0xB8BDA50F;
            _data[056] = 0x2802B89E;
            _data[057] = 0x5F058808;
            _data[058] = 0xC60CD9B2;
            _data[059] = 0xB10BE924;
            _data[060] = 0x2F6F7C87;
            _data[061] = 0x58684C11;
            _data[062] = 0xC1611DAB;
            _data[063] = 0xB6662D3D;
            _data[064] = 0x76DC4190;
            _data[065] = 0x01DB7106;
            _data[066] = 0x98D220BC;
            _data[067] = 0xEFD5102A;
            _data[068] = 0x71B18589;
            _data[069] = 0x06B6B51F;
            _data[070] = 0x9FBFE4A5;
            _data[071] = 0xE8B8D433;
            _data[072] = 0x7807C9A2;
            _data[073] = 0x0F00F934;
            _data[074] = 0x9609A88E;
            _data[075] = 0xE10E9818;
            _data[076] = 0x7F6A0DBB;
            _data[077] = 0x086D3D2D;
            _data[078] = 0x91646C97;
            _data[079] = 0xE6635C01;
            _data[080] = 0x6B6B51F4;
            _data[081] = 0x1C6C6162;
            _data[082] = 0x856530D8;
            _data[083] = 0xF262004E;
            _data[084] = 0x6C0695ED;
            _data[085] = 0x1B01A57B;
            _data[086] = 0x8208F4C1;
            _data[087] = 0xF50FC457;
            _data[088] = 0x65B0D9C6;
            _data[089] = 0x12B7E950;
            _data[090] = 0x8BBEB8EA;
            _data[091] = 0xFCB9887C;
            _data[092] = 0x62DD1DDF;
            _data[093] = 0x15DA2D49;
            _data[094] = 0x8CD37CF3;
            _data[095] = 0xFBD44C65;
            _data[096] = 0x4DB26158;
            _data[097] = 0x3AB551CE;
            _data[098] = 0xA3BC0074;
            _data[099] = 0xD4BB30E2;
            _data[100] = 0x4ADFA541;
            _data[101] = 0x3DD895D7;
            _data[102] = 0xA4D1C46D;
            _data[103] = 0xD3D6F4FB;
            _data[104] = 0x4369E96A;
            _data[105] = 0x346ED9FC;
            _data[106] = 0xAD678846;
            _data[107] = 0xDA60B8D0;
            _data[108] = 0x44042D73;
            _data[109] = 0x33031DE5;
            _data[110] = 0xAA0A4C5F;
            _data[111] = 0xDD0D7CC9;
            _data[112] = 0x5005713C;
            _data[113] = 0x270241AA;
            _data[114] = 0xBE0B1010;
            _data[115] = 0xC90C2086;
            _data[116] = 0x5768B525;
            _data[117] = 0x206F85B3;
            _data[118] = 0xB966D409;
            _data[119] = 0xCE61E49F;
            _data[120] = 0x5EDEF90E;
            _data[121] = 0x29D9C998;
            _data[122] = 0xB0D09822;
            _data[123] = 0xC7D7A8B4;
            _data[124] = 0x59B33D17;
            _data[125] = 0x2EB40D81;
            _data[126] = 0xB7BD5C3B;
            _data[127] = 0xC0BA6CAD;
            _data[128] = 0xEDB88320;
            _data[129] = 0x9ABFB3B6;
            _data[130] = 0x03B6E20C;
            _data[131] = 0x74B1D29A;
            _data[132] = 0xEAD54739;
            _data[133] = 0x9DD277AF;
            _data[134] = 0x04DB2615;
            _data[135] = 0x73DC1683;
            _data[136] = 0xE3630B12;
            _data[137] = 0x94643B84;
            _data[138] = 0x0D6D6A3E;
            _data[139] = 0x7A6A5AA8;
            _data[140] = 0xE40ECF0B;
            _data[141] = 0x9309FF9D;
            _data[142] = 0x0A00AE27;
            _data[143] = 0x7D079EB1;
            _data[144] = 0xF00F9344;
            _data[145] = 0x8708A3D2;
            _data[146] = 0x1E01F268;
            _data[147] = 0x6906C2FE;
            _data[148] = 0xF762575D;
            _data[149] = 0x806567CB;
            _data[150] = 0x196C3671;
            _data[151] = 0x6E6B06E7;
            _data[152] = 0xFED41B76;
            _data[153] = 0x89D32BE0;
            _data[154] = 0x10DA7A5A;
            _data[155] = 0x67DD4ACC;
            _data[156] = 0xF9B9DF6F;
            _data[157] = 0x8EBEEFF9;
            _data[158] = 0x17B7BE43;
            _data[159] = 0x60B08ED5;
            _data[160] = 0xD6D6A3E8;
            _data[161] = 0xA1D1937E;
            _data[162] = 0x38D8C2C4;
            _data[163] = 0x4FDFF252;
            _data[164] = 0xD1BB67F1;
            _data[165] = 0xA6BC5767;
            _data[166] = 0x3FB506DD;
            _data[167] = 0x48B2364B;
            _data[168] = 0xD80D2BDA;
            _data[169] = 0xAF0A1B4C;
            _data[170] = 0x36034AF6;
            _data[171] = 0x41047A60;
            _data[172] = 0xDF60EFC3;
            _data[173] = 0xA867DF55;
            _data[174] = 0x316E8EEF;
            _data[175] = 0x4669BE79;
            _data[176] = 0xCB61B38C;
            _data[177] = 0xBC66831A;
            _data[178] = 0x256FD2A0;
            _data[179] = 0x5268E236;
            _data[180] = 0xCC0C7795;
            _data[181] = 0xBB0B4703;
            _data[182] = 0x220216B9;
            _data[183] = 0x5505262F;
            _data[184] = 0xC5BA3BBE;
            _data[185] = 0xB2BD0B28;
            _data[186] = 0x2BB45A92;
            _data[187] = 0x5CB36A04;
            _data[188] = 0xC2D7FFA7;
            _data[189] = 0xB5D0CF31;
            _data[190] = 0x2CD99E8B;
            _data[191] = 0x5BDEAE1D;
            _data[192] = 0x9B64C2B0;
            _data[193] = 0xEC63F226;
            _data[194] = 0x756AA39C;
            _data[195] = 0x026D930A;
            _data[196] = 0x9C0906A9;
            _data[197] = 0xEB0E363F;
            _data[198] = 0x72076785;
            _data[199] = 0x05005713;
            _data[200] = 0x95BF4A82;
            _data[201] = 0xE2B87A14;
            _data[202] = 0x7BB12BAE;
            _data[203] = 0x0CB61B38;
            _data[204] = 0x92D28E9B;
            _data[205] = 0xE5D5BE0D;
            _data[206] = 0x7CDCEFB7;
            _data[207] = 0x0BDBDF21;
            _data[208] = 0x86D3D2D4;
            _data[209] = 0xF1D4E242;
            _data[210] = 0x68DDB3F8;
            _data[211] = 0x1FDA836E;
            _data[212] = 0x81BE16CD;
            _data[213] = 0xF6B9265B;
            _data[214] = 0x6FB077E1;
            _data[215] = 0x18B74777;
            _data[216] = 0x88085AE6;
            _data[217] = 0xFF0F6A70;
            _data[218] = 0x66063BCA;
            _data[219] = 0x11010B5C;
            _data[220] = 0x8F659EFF;
            _data[221] = 0xF862AE69;
            _data[222] = 0x616BFFD3;
            _data[223] = 0x166CCF45;
            _data[224] = 0xA00AE278;
            _data[225] = 0xD70DD2EE;
            _data[226] = 0x4E048354;
            _data[227] = 0x3903B3C2;
            _data[228] = 0xA7672661;
            _data[229] = 0xD06016F7;
            _data[230] = 0x4969474D;
            _data[231] = 0x3E6E77DB;
            _data[232] = 0xAED16A4A;
            _data[233] = 0xD9D65ADC;
            _data[234] = 0x40DF0B66;
            _data[235] = 0x37D83BF0;
            _data[236] = 0xA9BCAE53;
            _data[237] = 0xDEBB9EC5;
            _data[238] = 0x47B2CF7F;
            _data[239] = 0x30B5FFE9;
            _data[240] = 0xBDBDF21C;
            _data[241] = 0xCABAC28A;
            _data[242] = 0x53B39330;
            _data[243] = 0x24B4A3A6;
            _data[244] = 0xBAD03605;
            _data[245] = 0xCDD70693;
            _data[246] = 0x54DE5729;
            _data[247] = 0x23D967BF;
            _data[248] = 0xB3667A2E;
            _data[249] = 0xC4614AB8;
            _data[250] = 0x5D681B02;
            _data[251] = 0x2A6F2B94;
            _data[252] = 0xB40BBE37;
            _data[253] = 0xC30C8EA1;
            _data[254] = 0x5A05DF1B;
            _data[255] = 0x2D02EF8D;
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
}
