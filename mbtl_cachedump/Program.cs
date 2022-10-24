using PropertyHook;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace mbtl_cachedump
{
    class Program
    {
        public class SampleHook : PHook
        {
            public PHPointer StringCodeAddr;
            public PHPointer OffsetCodeAddr;
            public PHPointer SizeCodeAddr;
            public PHPointer StringPointer = null;  

            public SampleHook() : base(5000, 5000, p => p.ProcessName == "MBTL")
            {
                StringCodeAddr = RegisterAbsoluteAOB("8D 8D F0 FD FF FF 83 C4 10 33 F6 8D 51 01 66 90 8A 01",-26);
                //OffsetCodeAddr = RegisterAbsoluteAOB("53 56 57 8B 7D 08 8B D9 8B 4D 0C 47", 0xd);
                OffsetCodeAddr = RegisterAbsoluteAOB("74 05 83 FA FF 75 05 BB 01 00 00 00 C6 06 00 85 DB 75 6C",22); //fuck it
                

                SizeCodeAddr = RegisterAbsoluteAOB("75 2D 38 46 65 74 17 38 46 6C 75 12 8B 46 68",18);
            }
        }

        static void Main(string[] args)
        {
            SampleHook MBTLHook = new SampleHook();
            MBTLHook.Start();

            Console.WriteLine("waiting for hook");
            while (!MBTLHook.Hooked && !MBTLHook.AOBScanSucceeded)
            {

            }
            Console.WriteLine("hook success");

            List<String> path_list = new List<String>();
            List<byte[]> offset_list = new List<byte[]>();
            List<byte[]> size_list = new List<byte[]>();

            int str_index = 0;
            int num_str = 0;
            int chr_offset = 0;
            while(MBTLHook.StringCodeAddr.ReadInt32(str_index) != 0)
            {
                MBTLHook.StringPointer = MBTLHook.CreateChildPointer(MBTLHook.StringCodeAddr,str_index);

                //Console.WriteLine("cp val "+MBTLHook.StringPointer.ReadInt32(0));

                while(MBTLHook.StringPointer.ReadByte(chr_offset) != 0)
                {
                    chr_offset++;
                }

                string path_string = MBTLHook.StringPointer.ReadString(0, System.Text.Encoding.UTF8, (uint)chr_offset, false);
                if( path_string.Length > 0 ) path_list.Add(path_string);

                str_index += 4;
                num_str++;
                chr_offset = 0;
            }


            Console.WriteLine("its "+MBTLHook.OffsetCodeAddr.Resolve());

            //process offset and size tables
            for(int i = 0; i < num_str; i++)
            {
                byte[] offset_byte = MBTLHook.OffsetCodeAddr.ReadBytes((i * 4)+0x40, 4);
                byte[] size_byte = MBTLHook.SizeCodeAddr.ReadBytes(i * 4, 4);

                offset_list.Add(offset_byte);
                size_list.Add(size_byte);
            }

            byte[] offset_array = offset_list.SelectMany(a => a).ToArray();
            byte[] size_array = size_list.SelectMany(a => a).ToArray();

            File.WriteAllBytes("Cache_OT.bin",offset_array);
            Console.WriteLine("Saved "+AppDomain.CurrentDomain.BaseDirectory+"Cache_OT.bin");

            File.WriteAllBytes("Cache_ST.bin",size_array);
            Console.WriteLine("Saved " + AppDomain.CurrentDomain.BaseDirectory + "Cache_ST.bin");

            File.WriteAllLines("Cache_FN.txt",path_list);
            Console.WriteLine("Saved " + AppDomain.CurrentDomain.BaseDirectory + "Cache_FN.txt");

            Console.WriteLine("stri "+ num_str);

            MBTLHook.Stop();
        }
    }
}
