using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KotOR_Files.AuroraParsers
{
    class BIFFObject
    {

        public struct _BIFFHeader
        {
            public char[] FileType;
            public char[] FileVersion;
            public UInt32 VariableResourceCount;
            public UInt32 FixedResourceCount;
            public UInt32 VariableTableOffset;
        }

        public struct _VResourceHeader
        {
            public UInt32 ID;
            public UInt32 Offset;
            public UInt32 FileSize;
            public UInt32 ResourceType;
        }

        //Not used in spec
        public struct _FResourceHeader
        {
            public UInt32 ID;
            public UInt32 Offset;
            public UInt32 PartCount;
            public UInt32 FileSize;
            public UInt32 ResourceType;
        }

        AuroraFile file;
        BinaryReader Reader;
        _BIFFHeader Header = new _BIFFHeader();
        List<_VResourceHeader> VariableResourceList = new List<_VResourceHeader>();

        public BIFFObject(AuroraFile file)
        {
            this.file = file;

        }

        public void Read()
        {

            file.Open();
            Reader = file.getReader();

            Header.FileType = Reader.ReadChars(4);
            Header.FileVersion = Reader.ReadChars(4);
            Header.VariableResourceCount = Reader.ReadUInt32();
            Header.FixedResourceCount = Reader.ReadUInt32();
            Header.VariableTableOffset = Reader.ReadUInt32();

            Reader.BaseStream.Position = Header.VariableTableOffset;
            for(int i = 0; i!=Header.VariableResourceCount; i++)
            {
                _VResourceHeader res = new _VResourceHeader();
                res.ID = Reader.ReadUInt32();
                res.Offset = Reader.ReadUInt32();
                res.FileSize = Reader.ReadUInt32();
                res.ResourceType = Reader.ReadUInt32();
                VariableResourceList.Add(res);
            }

            file.Close();

        }

        public _VResourceHeader findFileByID(UInt32 id)
        {
            System.Diagnostics.Debug.Write("Finding: "+id);
            foreach(_VResourceHeader res in VariableResourceList)
            {
                if(res.ID == id)
                {
                    return res;
                }
            }

            return new _VResourceHeader();
        }

        public AuroraFile ExtractVResource(_VResourceHeader fileHeader, KEYObject._KeyTable key)
        {
            byte[] bytes = { 0 };
            file.Open();
            Reader = file.getReader();
            if (fileHeader.Offset != null)
            {
                Reader.BaseStream.Position = fileHeader.Offset;
                bytes = Reader.ReadBytes((int)fileHeader.FileSize);

                file.Close();
            }

            return new AuroraFile(bytes, new string(key.ResRef), key.ResourceType);

        }

        public AuroraFile ExtractFile (String filename, UInt32 restype)
        {
            byte[] bytes = { 0 };

            KEYObject._KeyTable key = Global.kotorKey.findFileKey(filename, restype);

            _VResourceHeader fileHeader = findFileByID(key.ResID);
            file.Open();
            Reader = file.getReader();
            if (fileHeader.Offset != null)
            {
                Reader.BaseStream.Position = fileHeader.Offset;
                bytes = Reader.ReadBytes((int)fileHeader.FileSize);

                file.Close();
            }

            return new AuroraFile(bytes, new string(key.ResRef), key.ResourceType);

        }

        public String getFilename()
        {
            return file.getFilename();
        }

        public List<_VResourceHeader> getResources()
        {
            return VariableResourceList;
        }

    }
}
