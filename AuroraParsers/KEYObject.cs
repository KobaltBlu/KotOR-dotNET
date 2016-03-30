using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KotOR_Files.AuroraParsers
{
    class KEYObject
    {

        public struct _KEYHeader
        {
            public char[] FileType;
            public char[] FileVersion;
            public UInt32 BIFCount;
            public UInt32 KeyCount;
            public UInt32 OffsetToFileTable;
            public UInt32 OffsetToKeyTable;
            public UInt32 BuildYear;
            public UInt32 BuildDay;
            public byte[] Reserved;
        }

        public struct _KeyTable
        {
            public char[] ResRef;
            public UInt16 ResourceType;
            public UInt32 ResID;
        }

        private AuroraFile file;
        private BinaryReader Reader;

        private _KEYHeader Header;
        private List<_KeyTable> KeysList;

        public KEYObject(AuroraFile file)
        {
            this.file = file;
            Header = new _KEYHeader();
            KeysList = new List<_KeyTable>();
        }

        public void Read()
        {
            this.file.Open();
            Reader = this.file.getReader();


            Header.FileType             = Reader.ReadChars(4);
            Header.FileVersion          = Reader.ReadChars(4);
            Header.BIFCount             = Reader.ReadUInt32();
            Header.KeyCount             = Reader.ReadUInt32();
            Header.OffsetToFileTable    = Reader.ReadUInt32();
            Header.OffsetToKeyTable     = Reader.ReadUInt32();
            Header.BuildYear            = Reader.ReadUInt32();
            Header.BuildDay             = Reader.ReadUInt32();
            Header.Reserved             = Reader.ReadBytes(32);

            Reader.BaseStream.Position = Header.OffsetToKeyTable;

            for(int i = 0; i!=Header.KeyCount; i++)
            {
                _KeyTable key = new _KeyTable();
                key.ResRef          = Reader.ReadChars(16);
                key.ResourceType    = Reader.ReadUInt16();
                key.ResID           = Reader.ReadUInt32();

                KeysList.Add(key);
            }


            this.file.Close();
        }


        public _KeyTable findFileKey(String ResRef, UInt32 ResourceType)
        {
            System.Diagnostics.Debug.WriteLine("Searching for: " + ResRef);
            foreach (_KeyTable key in KeysList)
            {
                if ( new string(key.ResRef).Replace("\0", string.Empty).Equals(ResRef) && key.ResourceType == ResourceType)
                {
                    System.Diagnostics.Debug.WriteLine("Found: "+new string(key.ResRef));
                    return key;
                }
            }
            return KeysList[0];

        }

    }
}
