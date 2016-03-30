using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using KotOR_Files.AuroraParsers;
using System.Diagnostics;

namespace KotOR_Files
{
    public class AuroraFile
    {

        public enum ResourceTypes
        {
            NA = 0x000F,
            bmp = 1,
            tga = 3,
            wav = 4,
            plt = 6,
            ini = 7,
            txt = 10,
            mdl = 2002,
            nss = 2009,
            ncs = 2010,
            are = 2012,
            set = 2013,
            ifo = 2014,
            bic = 2015,
            wok = 2016,
            _2da = 2017,
            txi = 2022,
            git = 2023,
            uti = 2025,
            utc = 2027,
            dlg = 2029,
            itp = 2030,
            utt = 2032,
            dds = 2033,
            uts = 2035,
            ltr = 2036,
            gff = 2037,
            fac = 2038,
            ute = 2040,
            utd = 2042,
            utp = 2045,
            dtf = 2045,
            gic = 2046,
            gui = 2047,
            utm = 2051,
            dwk = 2052,
            pwk = 2053,
            jrl = 2056,
            sav = 2057,
            utw = 2058,
            ssf = 2060,
            hak = 2061,
            nwm = 2062,
            bik = 2063,
            ptm = 2065,
            ptt = 2066,

            lyt = 3000,
            vis = 3001,
            rim = 3002,
            pth = 3003,
            lip = 3004,
            bwm = 3005,
            txb = 3006,
            tpc = 3007,
            mdx = 3008,
            rsv = 3009,
            sig = 3010,
            xbx = 3011,

            erf = 9997,
            bif = 9998,
            key = 9999

        }


        private FileStream fileStream;
        private MemoryStream memoryStream;
        private BinaryReader Reader;
        private StreamReader StreamReader;
        private byte[] bytes;

        private String name = "";
        private String ext = "";
        private String path = null;
        private Encoding encoding = Encoding.ASCII;

        public Boolean isText = false;

        public AuroraFile(String path)
        {
            this.ext = Path.GetExtension(path);
            this.name = Path.GetFileNameWithoutExtension(path);
            this.path = path;
        }

        public AuroraFile(byte[] bytes, String name, int restype)
        {
            this.bytes = bytes;
            this.ext = "."+((ResourceTypes)restype).ToString();
            this.name = name;

            Debug.WriteLine(this.ext);
        }


        public String getFilename()
        {
            return this.name;
        }

        public String getExt()
        {
            return this.ext;
        }

        public String getPath()
        {
            return this.path;
        }

        public void Open()
        {
            if(this.path != null)
            {
                fileStream = new FileStream(this.getPath(), FileMode.Open);
                if(isText)
                    StreamReader = new StreamReader(fileStream, encoding);
                else
                    Reader = new BinaryReader(fileStream, encoding);
            }
            else
            {
                memoryStream = new MemoryStream(bytes);
                Reader = new BinaryReader(memoryStream, encoding);
            }
        }

        public void setEncoding(Encoding encoding)
        {
            this.encoding = encoding;
        }

        public void Close()
        {
            if (this.path != null)
            {
                fileStream.Dispose();
                if(isText)
                    StreamReader.Dispose();
                else
                    Reader.Dispose();
            }
            else
            {
                memoryStream.Dispose();
                Reader.Dispose();
            }
        }

        public FileStream getFileStream()
        {
            return fileStream;
        }

        public MemoryStream getMemoryStream()
        {
            return memoryStream;
        }

        public BinaryReader getReader()
        {
            return Reader;
        }

        public StreamReader getStreamReader()
        {
            return StreamReader;
        }

        public byte[] getContents()
        {
            return bytes;
        }

        public void Export(String export_dir)
        {
            if (bytes != null)
            {

                FileStream fs = new FileStream(Path.Combine(export_dir, getFilename() + getExt()), FileMode.Create, FileAccess.Write);
                BinaryWriter bw = new BinaryWriter(fs);

                bw.Write(bytes);

                bw.Close();
                fs.Close();
            }
        }

        public void Export(String export_dir, String filename)
        {
            if (bytes != null)
            {

                FileStream fs = new FileStream(Path.Combine(export_dir, filename + getExt()), FileMode.Create, FileAccess.Write);
                BinaryWriter bw = new BinaryWriter(fs);

                bw.Write(bytes);

                bw.Close();
                fs.Close();
            }
        }

        public void Dispose()
        {
            bytes = null;
            fileStream = null;
            memoryStream = null;
            Reader = null;
        }

        public static void ReadValue(BinaryReader stream, out UInt32 value)
        {
            value = stream.ReadUInt32();
        }

        public static void ReadValue(BinaryReader stream, out float value)
        {
            value = stream.ReadSingle();
        }

        /*public static void ReadArray(BinaryReader stream,
                      UInt32 offset, UInt32 count,  out values)//std::vector<T>
        {

            
        }*/

        public static void ReadArray(BinaryReader stream,
                              UInt32 offset, UInt32 count, ref UInt32[] values)//vector<UInt32>
        {
            long pos = stream.BaseStream.Position;
            stream.BaseStream.Position = offset;

            //values.resize(count);
            for (UInt32 i = 0; i < count; i++)
            {
                UInt32 val;
                
                AuroraFile.ReadValue(stream, out val);
                Debug.WriteLine("ReadValue: " + val);
                values[i] = val;
            }

            stream.BaseStream.Position = pos;
        }
        public static void ReadArray(BinaryReader stream, UInt32 offset, UInt32 count, ref float[] values)//vector<float>
        {
            long pos = stream.BaseStream.Position;
            stream.BaseStream.Position = offset;

            //values.resize(count);
            for (UInt32 i = 0; i < count; i++)
                AuroraFile.ReadValue(stream, out values[i]);

            stream.BaseStream.Position = pos;
        }

        //Gets the Array Offset & Item Count
        public static void ReadArrayDef(BinaryReader stream, out UInt32 offset, out UInt32 count)
        {

            offset = stream.ReadUInt32();

            UInt32 usedCount = stream.ReadUInt32();
            UInt32 allocatedCount = stream.ReadUInt32();

            if (usedCount != allocatedCount)
                throw new Exception("Model::readArrayDef(): usedCount != allocatedCount ("+ usedCount + ", "+ allocatedCount + ")");

            count = usedCount;
        }

        public static void readStrings(BinaryReader stream, uint[] offsets, uint offset, out string[] strings) {

            long pos = stream.BaseStream.Position;

            strings = new string[offsets.Length];

            Debug.WriteLine("Reading Strings");

            for (int i = 0; i != offsets.Length; i++)
            {
                stream.BaseStream.Position = offset + offsets[i];

                string tmpName = "";
                char c;

                while ((int)(c = stream.ReadChar()) != 0)
                    tmpName = tmpName + c;

                strings[i] = tmpName;
                //Debug.WriteLine(tmpName);
                //strings.push_back(Common::readString(mdl, Common::kEncodingASCII));
            }

            stream.BaseStream.Position = pos;
}

    }
}
