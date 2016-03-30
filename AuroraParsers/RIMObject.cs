using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KotOR_Files.AuroraParsers
{
    class RIMObject
    {

        public struct _RIMHEader
        {
            public char[] FileType;                 //Char 4
            public char[] FileVersion;              //Char 4
            public UInt32 Unknown_1;
            public UInt32 EntryCount;
            public UInt32 OffsetToKeyList;
            public byte[] Reserved;                 //Byte 64
        }

        public struct _RIMKey
        {
            public char[] ResRef;             //Char 16 NULL terminated
            public UInt16 ResType;
            public UInt16 ResID;              //Resource ID
            public UInt16 Reserved;
            public UInt32 Offset;
            public UInt32 Length;
        }

        public struct _RIMResource
        {
            public char[] ResRef;             //Char 16 NULL terminated
            public UInt16 ResType;
            public UInt32 Length;
            public byte[] bytes;
        }

        private AuroraFile file;
        private BinaryReader Reader;
        private byte[] ByteBuffer;

        private _RIMHEader Header = new _RIMHEader();
        private List<_RIMKey> Keys = new List<_RIMKey>();


        public RIMObject(AuroraFile file)
        {
            this.file = file;
        }

        public void Read()
        {
            file.Open();
            Reader = file.getReader();

            Header.FileType = Reader.ReadChars(4);
            Header.FileVersion = Reader.ReadChars(4);
            Header.Unknown_1 = Reader.ReadUInt32();
            Header.EntryCount = Reader.ReadUInt32();
            Header.OffsetToKeyList = Reader.ReadUInt32();
            Header.Reserved = Reader.ReadBytes(100); //Reserved bytes

            Reader.BaseStream.Position = Header.OffsetToKeyList;

            for(int i = 0; i!= Header.EntryCount; i++)
            {

                _RIMKey key = new _RIMKey();
                key.ResRef = Reader.ReadChars(16);
                key.ResType = Reader.ReadUInt16();
                key.ResID = Reader.ReadUInt16();
                key.Reserved = Reader.ReadUInt16();
                key.Offset = Reader.ReadUInt32();
                key.Length = Reader.ReadUInt32();
                Keys.Add(key);

                Debug.WriteLine(new string(key.ResRef));

            }

            file.Close();
        }

        public byte[] getRawResource(_RIMKey _key)
        {
            file.Open();
            Reader = file.getReader();
            Reader.BaseStream.Position = _key.Offset;
            byte[] bytes = Reader.ReadBytes((int)_key.Length);
            file.Close();

            return bytes;

        }

        public _RIMKey getResourceByKey(String key, int restype)
        {

            Debug.WriteLine("Searching: " + file.getFilename());
            foreach (_RIMKey _key in Keys)
            {
                //Debug.WriteLine("Resource Name: " + new string(_key.ResRef));
                if (new string(_key.ResRef).Replace("\0", string.Empty) == key && _key.ResType == (ushort)restype)
                {
                    
                    return _key;
                }
            }
            return new _RIMKey();
        }

        public AuroraFile getFile()
        {
            return file;
        }

    }
}
