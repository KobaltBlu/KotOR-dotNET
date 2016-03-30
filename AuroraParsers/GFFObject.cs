using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;

namespace KotOR_Files.AuroraParsers
{
    public class GFFObject
    {

        public struct _GFFHeader
        {
            public string FileType;
            public string FileVersion;
            public int StructOffset;
            public int StructCount;
            public int FieldOffset;
            public int FieldCount;
            public int LabelOffset;
            public int LabelCount;
            public int FieldDataOffset;
            public int FieldDataCount;
            public int FieldIndicesOffset;
            public int FieldIndicesCount;
            public int ListIndicesOffset;
            public int ListIndicesCount;
        }

        public struct _GFFStruct
        {
            public int Type;
            public int DataOrDataOffset;
            public int FieldCount;
        }

        public struct _GFFField
        {
            public int Type;
            public int LabelIndex;
            public byte[] DataOrDataOffset;
            public string Value; //Holds the Fields value in text form Type will be used to return it to its right form as in INT Float Double etc.
            public byte[] ComplexData;
            public int Index;

            public _GFFField(int type, int label, byte[] data, string val, int index)
            {
                Type = type;
                LabelIndex = label;
                DataOrDataOffset = data;
                Value = val;
                Index = index;
                ComplexData = new byte[0];
            }

            public string ReadLabel()
            {
                return GetLabel(LabelIndex);
            }

            public string ReadValue()
            {
                switch (Type)
                {
                    case (int)DataTypes.CExoLocString:

                        System.IO.Stream stream = new System.IO.MemoryStream(ComplexData);

                        BinaryReader CExoLocReader = new BinaryReader(stream);
                        //Console.WriteLine("Len: " + CExoLocReader.BaseStream.Length + " Bytes: " + ComplexData.Length);
                        int StringRef = CExoLocReader.ReadInt32();
                        int StringCount = CExoLocReader.ReadInt32();

                        for (int j = 0; j != StringCount; j++)
                        {

                            int StringId = CExoLocReader.ReadInt32();
                            int StringLen = CExoLocReader.ReadInt32();

                            byte[] localString = CExoLocReader.ReadBytes(StringLen);

                            int stringID = StringId;
                            bool feminine = (0x1 & stringID) == 0x1;
                            int languageID = stringID >> 1;

                            string gender = "Male";

                            if (feminine)
                            {
                                gender = "Female";
                            }

                            return System.Text.Encoding.UTF8.GetString(localString);

                        }

                        //Loop through


                        CExoLocReader.Close();
                        CExoLocReader.Dispose();
                        break;
                }

                return "";
            }

            public string RetVal()
            {
                return "";
            }
        }

        public enum DataTypes
        {
            BYTE = 0,
            CHAR = 1,
            WORD = 2,
            SHORT = 3,
            DWORD = 4,
            INT = 5,
            DWORD64 = 6,
            INT64 = 7,
            FLOAT = 8,
            DOUBLE = 9,
            CExoString = 10,
            ResRef = 11,
            CExoLocString = 12,
            VOID = 13,
            Struct = 14,
            List = 15
        }

        public enum LanguageIDs
        {
            English = 0,
            French = 1,
            German = 2,
            Italian = 3,
            Spanish = 4,
            Polish = 5,
            Korean = 128,
            [Description("Chinese Traditional")]
            ChineseTraditional = 129,
            [Description("Chinese Simplified")]
            ChineseSimplified = 130,
            Japanese = 131,
        }

        private AuroraFile file;
        private BinaryReader Reader;
        static _GFFHeader Header;
        static List<_GFFStruct> StructArray = new List<_GFFStruct>();
        static List<_GFFField> ListArray = new List<_GFFField>();
        static List<String> LabelArray = new List<String>();

        static List<_GFFField> FieldArray = new List<_GFFField>(); //Array of all the fields

        static List<int[]> ListIndiciesArray = new List<int[]>();

        public TreeNode TreeData = new TreeNode();

        private byte[] ByteBuffer;

        private bool debug = false;

        public GFFObject(AuroraFile file)
        {
            this.file = file;
            Header = new _GFFHeader();
            StructArray = new List<_GFFStruct>();
            ListArray = new List<_GFFField>();
            LabelArray = new List<String>();

            FieldArray = new List<_GFFField>(); //Array of all the fields

            TreeData = new TreeNode();
        }

        //Opens the file
        public void Open()
        {
            file.Open();
            Reader = file.getReader();
            Reader.BaseStream.Position = 0;
        }

        public string GetFileType()
        {
            return Header.FileType;
        }

        public string GetFileVersion()
        {
            return Header.FileVersion;
        }

        private void Seek(int bytes)
        {
            Reader.BaseStream.Position = bytes;
        }

        //Reads the binary data
        public void Read()
        {

            this.Open();

            if (Reader != null)
            {
                Seek(56);
                ByteBuffer = Reader.ReadBytes((int)Reader.BaseStream.Length - 56);

                Seek(0);

                //Get HEADER Data

                Header = ReadHeader();

                //END HEADER

                Reader.BaseStream.Position = Header.StructOffset;

                for (int i = 0; i < Header.StructCount; i++)
                {

                    _GFFStruct tmp = new _GFFStruct();

                    tmp.Type = Reader.ReadInt32();
                    tmp.DataOrDataOffset = Reader.ReadInt32();
                    tmp.FieldCount = Reader.ReadInt32();

                    StructArray.Add(tmp);

                }


                //Start Labels

                //Place all the labels in the label array

                long OriginalPos = Reader.BaseStream.Position;

                Reader.BaseStream.Position = Header.LabelOffset;

                for (int i = 0; i < Header.LabelCount; i++)
                {
                    string str = new string(Reader.ReadChars(16)).Replace("\0", string.Empty); ;
                    LabelArray.Add(str);
                }

                Reader.BaseStream.Position = OriginalPos;

                //End Labels


                //Start Fields

                for (int i = 0; i < Header.FieldCount; i++)
                {

                    int Type = Reader.ReadInt32();
                    int Label = Reader.ReadInt32();

                    byte[] Data = Reader.ReadBytes(4); // Field data is 4Bytes long longer data is stored elsewhere but this data is used to find it

                    int Index = i;

                    _GFFField newField = new _GFFField(Type, Label, Data, ReadFieldData(Data, Type), Index);
                    if (debug)
                    {
                        switch (Type)
                        {
                            case (int)DataTypes.BYTE:
                                Console.WriteLine("[Byte] Name: " + GetLabel(Label) + " Value: " + BitConverter.ToInt32(Data, 0));
                                break;
                            case (int)DataTypes.CExoLocString:
                                newField.ComplexData = GetCExoLocString(BitConverter.ToInt32(Data, 0));
                                Console.WriteLine("[CExoLocString] Name: " + GetLabel(Label) + " Value: Complex");
                                break;
                            case (int)DataTypes.CExoString:
                                Console.WriteLine("[CExoString] Name: " + GetLabel(Label) + " Value: " + GetCExoString(BitConverter.ToInt32(Data, 0)));
                                break;
                            case (int)DataTypes.CHAR:
                                Console.WriteLine("[Char] Name: " + GetLabel(Label) + " Value: " + BitConverter.ToInt32(Data, 0));
                                break;
                            case (int)DataTypes.DOUBLE:
                                Console.WriteLine("[Double] Name: " + GetLabel(Label) + " Value: " + GetDouble(BitConverter.ToInt32(Data, 0)));
                                break;
                            case (int)DataTypes.DWORD:
                                Console.WriteLine("[DWORD] Name: " + GetLabel(Label) + " Value: " + BitConverter.ToInt32(Data, 0));
                                break;
                            case (int)DataTypes.DWORD64:
                                Console.WriteLine("[DWORD64] Name: " + GetLabel(Label) + " Value: " + GetDword64(BitConverter.ToInt32(Data, 0)));
                                break;
                            case (int)DataTypes.FLOAT:
                                Console.WriteLine("[Float] Name: " + GetLabel(Label) + " Value: " + BitConverter.ToSingle(Data, 0));
                                break;
                            case (int)DataTypes.INT:
                                Console.WriteLine("[Int] Name: " + GetLabel(Label) + " Value: " + BitConverter.ToInt32(Data, 0));
                                break;
                            case (int)DataTypes.INT64:
                                Console.WriteLine("[Int64] Name: " + GetLabel(Label) + " Value: " + BitConverter.ToInt32(Data, 0));
                                break;
                            case (int)DataTypes.List:
                                Console.WriteLine("List: " + BitConverter.ToInt32(Data, 0)); // Offset in bytes realative to the List Indices Array Start Byte
                                break;
                            case (int)DataTypes.ResRef:
                                Console.WriteLine("[ResRef] Name: " + GetLabel(Label) + " Value: " + GetResRef(BitConverter.ToInt32(Data, 0)));
                                break;
                            case (int)DataTypes.SHORT:
                                Console.WriteLine("[Short] Name: " + GetLabel(Label) + " Value: " + BitConverter.ToInt32(Data, 0));
                                break;
                            case (int)DataTypes.Struct:
                                Console.WriteLine("Struct: " + BitConverter.ToInt32(Data, 0)); //Index of the struct
                                break;
                            case (int)DataTypes.VOID:
                                Console.WriteLine("[Void] Name: " + GetLabel(Label) + " Value: " + BitConverter.ToInt32(Data, 0));
                                break;
                            case (int)DataTypes.WORD:
                                Console.WriteLine("[Word] Name: " + GetLabel(Label) + " Value: " + BitConverter.ToInt32(Data, 0));
                                break;
                        }
                    }
                    else
                    {
                        switch (Type)
                        {
                            case (int)DataTypes.CExoLocString:
                                newField.ComplexData = GetCExoLocString(BitConverter.ToInt32(Data, 0));
                                break;
                        }
                    }

                    FieldArray.Add(newField);

                }

                //EOF

                //PrintableData();

                file.Close(); //Close the file because we are done reading data...

            }
            else
            {

            }

        }

        public void SaveGFF()
        {
            //Test out the save function by writing out the header
            //FileStream fs = new FileStream("header.tmp", FileMode.Create);
            //BinaryWriter br = new BinaryWriter(fs);

            //Create buffers
            byte[] _StructArray = new byte[0];
            byte[] _FieldArray = new byte[0];
            byte[] _FieldDataBlock = new byte[0]; //This byte array holds all the Field data that is bigger that 32Byte Dwords

            char[] FileType = Header.FileType.ToCharArray(0, Header.FileType.Length);
            char[] FileVersion = Header.FileVersion.ToCharArray(0, Header.FileVersion.Length);

            /*br.Write(FileType);
            br.Write(FileVersion);
            br.Write((UInt32)Header.StructOffset);
            br.Write((UInt32)Header.StructCount);
            br.Write((UInt32)Header.FieldOffset);
            br.Write((UInt32)Header.FieldCount);
            br.Write((UInt32)Header.LabelOffset);
            br.Write((UInt32)Header.LabelCount);
            br.Write((UInt32)Header.FieldDataOffset);
            br.Write((UInt32)Header.FieldDataCount);
            br.Write((UInt32)Header.FieldIndicesOffset);
            br.Write((UInt32)Header.FieldIndicesCount);
            br.Write((UInt32)Header.ListIndicesOffset);
            br.Write((UInt32)Header.ListIndicesCount);*/

            //Create the Struct Array
            for (int i = 0; i != StructArray.Count - 1; i++)
            {
                _GFFStruct _Struct = StructArray[i];

                byte[] structBuffer = new byte[12];
                Buffer.BlockCopy(new int[_Struct.Type, _Struct.DataOrDataOffset, _Struct.FieldCount], 0, structBuffer, 0, 12);

                Buffer.BlockCopy(structBuffer, 0, _StructArray, 0, _StructArray.Length + 12);
            }

            //Create the Field Array
            for (int i = 0; i != FieldArray.Count - 1; i++)
            {
                _GFFField _Field = FieldArray[i];

                byte[] fieldBuffer = new byte[12];

                int DataOrDataOffset = BitConverter.ToInt32(_Field.DataOrDataOffset, 0);

                //Check to see if data is complex
                switch (_Field.Type)
                {

                    //Complex data must be stored in the FieldDataBlock and a reference to the byte offset stored in the field struct
                    case (int)DataTypes.DWORD64:
                        DataOrDataOffset = _FieldDataBlock.Length; //Record the FieldDataBlock current offset

                        UInt64 DWORD = UInt64.Parse(_Field.Value); //Convert the data string back to a DWORD
                        byte[] DwordBytes = BitConverter.GetBytes(DWORD); //Convert the UINT64 to an 8Byte Array

                        Buffer.BlockCopy(DwordBytes, 0, _FieldDataBlock, _FieldDataBlock.Length, 12); //Add the DWORD Bytes to the FIELDDATABLOCK

                        break;
                    case (int)DataTypes.INT64:

                        DataOrDataOffset = _FieldDataBlock.Length; //Record the FieldDataBlock current offset

                        Int64 _INT64 = Int64.Parse(_Field.Value); //Convert the data string back to a INT64
                        byte[] _INT64Bytes = BitConverter.GetBytes(_INT64); //Convert the INT64 to an 8Byte Array

                        Buffer.BlockCopy(_INT64Bytes, 0, _FieldDataBlock, _FieldDataBlock.Length, 12); //Add the INT64 Bytes to the FIELDDATABLOCK

                        break;
                    case (int)DataTypes.DOUBLE:

                        DataOrDataOffset = _FieldDataBlock.Length; //Record the FieldDataBlock current offset

                        Double DOUBLE = Double.Parse(_Field.Value); //Convert the data string back to a Double
                        byte[] DOUBLEBytes = BitConverter.GetBytes(DOUBLE); //Convert the Double to an 8Byte Array

                        Buffer.BlockCopy(DOUBLEBytes, 0, _FieldDataBlock, _FieldDataBlock.Length, 12); //Add the Double Bytes to the FIELDDATABLOCK

                        break;
                    case (int)DataTypes.CExoString:

                        DataOrDataOffset = _FieldDataBlock.Length; //Record the FieldDataBlock current offset

                        string CExoString = _Field.Value;

                        byte[] CExoStringBytes = System.Text.Encoding.UTF8.GetBytes(CExoString); //Get the bytes of the CExoString

                        byte[] CExoStringsize = BitConverter.GetBytes((UInt32)CExoStringBytes.Length); //Get the byte size of the CExoString 4 Bytes long

                        byte[] nCExoStringBytes = new byte[CExoStringsize.Length + CExoStringBytes.Length];

                        System.Buffer.BlockCopy(CExoStringsize, 0, nCExoStringBytes, 0, CExoStringsize.Length);
                        System.Buffer.BlockCopy(CExoStringBytes, 0, nCExoStringBytes, CExoStringsize.Length, CExoStringBytes.Length);

                        Buffer.BlockCopy(nCExoStringBytes, 0, _FieldDataBlock, _FieldDataBlock.Length, nCExoStringBytes.Length); //Add the RESREF Bytes to the FIELDDATABLOCK

                        break;
                    case (int)DataTypes.ResRef:

                        DataOrDataOffset = _FieldDataBlock.Length; //Record the FieldDataBlock current offset

                        string str = _Field.Value;
                        str = str.Substring(0, 16);

                        byte[] RESREFBytes = System.Text.Encoding.UTF8.GetBytes(str); //Get the bytes of the ResRef

                        byte size = Convert.ToByte(RESREFBytes.Length); //Get the byte size of the ResRef


                        byte[] nRESREFBytes = new byte[RESREFBytes.Length + 1];
                        RESREFBytes.CopyTo(nRESREFBytes, 1);
                        nRESREFBytes[0] = size; //Prepend the size byte to the RESREF Array
                        RESREFBytes = nRESREFBytes;


                        Buffer.BlockCopy(RESREFBytes, 0, _FieldDataBlock, _FieldDataBlock.Length, RESREFBytes.Length); //Add the RESREF Bytes to the FIELDDATABLOCK





                        break;
                    case (int)DataTypes.CExoLocString:

                        DataOrDataOffset = _FieldDataBlock.Length; //Record the FieldDataBlock current offset

                        Buffer.BlockCopy(_Field.ComplexData, 0, _FieldDataBlock, _FieldDataBlock.Length, _Field.ComplexData.Length); //Add the CExoLocString Bytes to the FIELDDATABLOCK

                        break;
                    case (int)DataTypes.VOID:

                        DataOrDataOffset = _FieldDataBlock.Length; //Record the FieldDataBlock current offset

                        break;
                        /*case (int)DataTypes.Struct:

                        break;
                        case (int)DataTypes.List:

                        break;*/
                }

                Buffer.BlockCopy(new int[_Field.Type, _Field.LabelIndex, DataOrDataOffset], 0, fieldBuffer, 0, 12); //Fill the Field Buffer with its data

                Buffer.BlockCopy(fieldBuffer, 0, _FieldArray, _FieldArray.Length, 12); //Add the Field Buffer to the Field Array
            }

            //Create the Labels array


            //Create the ListIndices


            //Test rest of file
            //br.Write(ByteBuffer);


            //br.Close();

        }

        //Reads and sets the files header data
        private _GFFHeader ReadHeader()
        {

            _GFFHeader tmp = new _GFFHeader();

            tmp.FileType = new string(Reader.ReadChars(4));
            tmp.FileVersion = new string(Reader.ReadChars(4));

            tmp.StructOffset = Reader.ReadInt32();
            tmp.StructCount = Reader.ReadInt32();
            tmp.FieldOffset = Reader.ReadInt32();
            tmp.FieldCount = Reader.ReadInt32();
            tmp.LabelOffset = Reader.ReadInt32();
            tmp.LabelCount = Reader.ReadInt32();
            tmp.FieldDataOffset = Reader.ReadInt32();
            tmp.FieldDataCount = Reader.ReadInt32();
            tmp.FieldIndicesOffset = Reader.ReadInt32();
            tmp.FieldIndicesCount = Reader.ReadInt32();
            tmp.ListIndicesOffset = Reader.ReadInt32();
            tmp.ListIndicesCount = Reader.ReadInt32();

            return tmp;
        }

        //Loops through all the retrieved data to see if it can create the data tree correctly. If so that means all the data was gathered correctly
        public void PrintableData()
        {
            file.Open();
            Reader = file.getReader();
            Console.WriteLine("\nPrinting Data:\n");
            //Start with the main struct and iterate tree view style following next child objects before proceding to the next element

            TreeData = new TreeNode("[STRUCT ID: -1]");
            TreeData.Tag = new _GFFField(14, 0, null, "STRUCT", -1);
            Console.WriteLine("[-] Struct[0]: \tChildren: " + StructArray[0].FieldCount + "\t Fields: " + StructArray[0].DataOrDataOffset);
            //Next loop through child elements of the main struct which will be the field because a struct has no direct child structs


            List<_GFFField> _fields = GetStructFields(StructArray[0].DataOrDataOffset, StructArray[0].FieldCount);
            printFields(_fields, 0, TreeData);




            for (int i = 1; i != StructArray.Count; i++)
            {
                //Console.WriteLine(" [+] Struct["+i+"]: \tChildren: " + StructArray[i].FieldCount+"\t Fields: " + StructArray[i].DataOrDataOffset);
            }
            file.Close();

        }

        private void printFields(List<_GFFField> fieldList, int indent, TreeNode Node)
        {
           
            for (int i = 0; i != fieldList.Count; i++)
            {

                _GFFField _Field = fieldList[i];

                string lbl = _Field.ReadLabel();
                int _type = _Field.Type;

                //Console.WriteLine(_type);

                TreeNode NewNode = new TreeNode("" + lbl.ToString() + " Type: Value: ");

                NewNode.Text = GetLabel(_Field.LabelIndex) + " [Type: " + ((DataTypes)_type) + "] Value: " + _Field.Value + " ";

                NewNode.Tag = _Field;

                //NewNode.Text = (lbl + " [Type: " + ((DataTypes)_type) + "] Value: " + ReadFieldData(_Field.DataOrDataOffset, _type));

                if (_type == (int)DataTypes.Struct)
                {
                    Console.WriteLine(ParseIndent(indent) + " -" + lbl + " : STRUCT");
                }
                else if (_type == (int)DataTypes.CExoLocString)
                {

                    byte[] ComplexData = _Field.ComplexData;

                    System.IO.Stream stream = new System.IO.MemoryStream(ComplexData);

                    BinaryReader CExoLocReader = new BinaryReader(stream);
                    Console.WriteLine("Len: " + CExoLocReader.BaseStream.Length + " Bytes: " + ComplexData.Length);
                    int StringRef = CExoLocReader.ReadInt32();
                    int StringCount = CExoLocReader.ReadInt32();

                    for (int j = 0; j != StringCount; j++)
                    {

                        int StringId = CExoLocReader.ReadInt32();
                        int StringLen = CExoLocReader.ReadInt32();

                        byte[] localString = CExoLocReader.ReadBytes(StringLen);

                        int stringID = StringId;
                        bool feminine = (0x1 & stringID) == 0x1;
                        int languageID = stringID >> 1;

                        string gender = "Male";

                        if (feminine)
                        {
                            gender = "Female";
                        }

                        TreeNode newSubString = new TreeNode("[LOCALSTRING] String Id: " + stringID + " (" + (LanguageIDs)languageID + " " + gender + ") " + System.Text.Encoding.UTF8.GetString(localString));
                        newSubString.Tag = new _GFFField((int)DataTypes.CExoLocString, 0, null, System.Text.Encoding.UTF8.GetString(localString), 0);
                        NewNode.Nodes.Add(newSubString);

                    }

                    //Loop through


                    CExoLocReader.Close();
                    CExoLocReader.Dispose();

                }
                else if (_type == (int)DataTypes.List)
                {

                    int[] listStructArray = GetListElements(BitConverter.ToInt32(_Field.DataOrDataOffset, 0));

                    //Console.WriteLine(ParseIndent(indent) + " -" + lbl + " : LIST #" + listStructArray.Length);

                    if (listStructArray.Length != 0)
                    {

                        int indentH = indent++;

                        for (int j = 0; j != listStructArray.Length; j++)
                        {

                            TreeNode newStruct = new TreeNode("[STRUCT ID: " + listStructArray[j] + "]");
                            newStruct.Tag = new _GFFField(14, 0, null, "STRUCT", listStructArray[j]);
                            NewNode.Nodes.Add(newStruct);

                            _GFFStruct _struct = StructArray[listStructArray[j]];


                            List<_GFFField> _nfields = GetStructFields(_struct.DataOrDataOffset, _struct.FieldCount);
                            printFields(_nfields, (indentH), newStruct);




                        }

                    }

                }
                else
                {

                    //Console.WriteLine(ParseIndent(indent) + " -" + lbl + " : " + ReadFieldData(_Field.DataOrDataOffset, _type));
                    NewNode.Text = lbl + " [Type: " + ((DataTypes)_type) + "] Value: " + ReadFieldData(_Field.DataOrDataOffset, _type);
                }

                Node.Nodes.Add(NewNode);
            }

        }

        private string ParseIndent(int i)
        {
            string str = "";

            if (i != 0)
            {
                for (int j = 0; j != i + 4; j++)
                {
                    str += " ";
                }
            }

            return str;
        }

        //Gets data from the FieldDataHeader
        private string GetResRef(int offset)
        {

            string ResRef = "";

            long OriginalPos = Reader.BaseStream.Position;//Store the original position of the reader object

            Reader.BaseStream.Position = (Header.FieldDataOffset + offset);

            int length = Reader.ReadByte();// Get the length of the string
            if (length != 0)
            {
                ResRef = new string(Reader.ReadChars(length));
            }

            Reader.BaseStream.Position = OriginalPos;//Return the reader position to the original

            return ResRef;
        }

        //Gets data from the FieldDataHeader
        private byte[] GetCExoLocString(int offset)
        {

            byte[] bytes;

            long OriginalPos = Reader.BaseStream.Position;//Store the original position of the reader object

            Reader.BaseStream.Position = (Header.FieldDataOffset + offset);

            int length = (int)Reader.ReadInt32();//Total Size of the GetCExoLocString

            bytes = Reader.ReadBytes(length);

            Reader.BaseStream.Position = OriginalPos;//Return the reader position to the original

            return bytes;
        }

        //Gets data from the FieldDataHeader
        private int GetCExoLocStringRef(int offset)
        {

            int StringRef;

            long OriginalPos = Reader.BaseStream.Position;//Store the original position of the reader object

            Reader.BaseStream.Position = (Header.FieldDataOffset + offset);

            int length = (int)Reader.ReadInt32();//Total Size of the GetCExoLocString

            StringRef = Reader.ReadInt32();

            Reader.BaseStream.Position = OriginalPos;//Return the reader position to the original

            return StringRef;
        }

        //Gets data from the FieldDataHeader
        private string GetCExoString(int offset)
        {

            string ResRef = "";

            long OriginalPos = Reader.BaseStream.Position;//Store the original position of the reader object

            Reader.BaseStream.Position = (Header.FieldDataOffset + offset);

            int length = (int)Reader.ReadInt32();// Get the length of the string
            if (length != 0)
            {
                ResRef = new string(Reader.ReadChars(length));
            }

            Reader.BaseStream.Position = OriginalPos;//Return the reader position to the original

            return ResRef;
        }

        //Gets data from the FieldDataHeader
        private string GetDword64(int offset)
        {

            string Dword64 = "";

            long OriginalPos = Reader.BaseStream.Position;//Store the original position of the reader object

            Reader.BaseStream.Position = (Header.FieldDataOffset + offset);

            Dword64 = Reader.ReadUInt64().ToString();

            Reader.BaseStream.Position = OriginalPos;//Return the reader position to the original

            return Dword64;
        }

        //Gets data from the FieldDataHeader
        private string GetDouble(int offset)
        {

            string Double = "";

            long OriginalPos = Reader.BaseStream.Position;//Store the original position of the reader object

            Reader.BaseStream.Position = (Header.FieldDataOffset + offset);

            Double = BitConverter.ToDouble(Reader.ReadBytes(8), 0).ToString();

            Reader.BaseStream.Position = OriginalPos;//Return the reader position to the original

            return Double;
        }

        public static string GetLabel(int offset)
        {
            return LabelArray[offset];
        }

        private string ReadFieldData(byte[] DataOrDataOffset, int Type)
        {
            string val = "";
            switch (Type)
            {
                case (int)DataTypes.BYTE:
                    val = BitConverter.ToInt32(DataOrDataOffset, 0).ToString();
                    break;
                case (int)DataTypes.CExoLocString:
                    val = GetCExoLocStringRef(BitConverter.ToInt32(DataOrDataOffset, 0)).ToString();
                    break;
                case (int)DataTypes.CExoString:
                    val = GetCExoString(BitConverter.ToInt32(DataOrDataOffset, 0));
                    break;
                case (int)DataTypes.CHAR:
                    val = BitConverter.ToInt32(DataOrDataOffset, 0).ToString();
                    break;
                case (int)DataTypes.DOUBLE:
                    val = GetDouble(BitConverter.ToInt32(DataOrDataOffset, 0)).ToString();
                    break;
                case (int)DataTypes.DWORD:
                    val = BitConverter.ToInt32(DataOrDataOffset, 0).ToString();
                    break;
                case (int)DataTypes.DWORD64:
                    val = GetDword64(BitConverter.ToInt32(DataOrDataOffset, 0)).ToString();
                    break;
                case (int)DataTypes.FLOAT:
                    val = BitConverter.ToSingle(DataOrDataOffset, 0).ToString();
                    break;
                case (int)DataTypes.INT:
                    val = BitConverter.ToInt32(DataOrDataOffset, 0).ToString();
                    break;
                case (int)DataTypes.INT64:
                    val = BitConverter.ToInt32(DataOrDataOffset, 0).ToString();
                    break;
                case (int)DataTypes.List:
                    val = BitConverter.ToInt32(DataOrDataOffset, 0).ToString(); // Offset in bytes realative to the List Indices Array Start Byte
                    break;
                case (int)DataTypes.ResRef:
                    val = GetResRef(BitConverter.ToInt32(DataOrDataOffset, 0)).ToString();
                    break;
                case (int)DataTypes.SHORT:
                    val = BitConverter.ToInt32(DataOrDataOffset, 0).ToString();
                    break;
                case (int)DataTypes.Struct:
                    val = BitConverter.ToInt32(DataOrDataOffset, 0).ToString(); //Index of the struct
                    break;
                case (int)DataTypes.VOID:
                    val = BitConverter.ToInt32(DataOrDataOffset, 0).ToString();
                    break;
                case (int)DataTypes.WORD:
                    val = BitConverter.ToInt32(DataOrDataOffset, 0).ToString();
                    break;
            }

            return val;
        }

        private int[] GetListElements(int offset)
        {

            long OriginalPos = Reader.BaseStream.Position;//Store the original position of the reader object

            Reader.BaseStream.Position = (Header.ListIndicesOffset + offset);

            int ListSize = Reader.ReadInt32();//The first 4 bytes indicate the size of the array

            int[] List = new int[ListSize];

            for (int i = 0; i != ListSize; i++)
            {
                List[i] = Reader.ReadInt32();
            }

            Reader.BaseStream.Position = OriginalPos;//Return the reader position to the original

            return List;
        }

        private List<_GFFField> GetStructFields(int offset, int count)
        {
            List<_GFFField> fields = new List<_GFFField>();
            if (count > 1)
            {

                long OriginalPos = Reader.BaseStream.Position;

                Seek(Header.FieldIndicesOffset + offset);



                for (int i = 0; i != count; i++)
                {
                    int faindex = Reader.ReadInt32();

                    fields.Add(FieldArray[faindex]);

                }

                Seek((int)OriginalPos);


            }
            else
            {

                fields.Add(FieldArray[offset]);

            }

            return fields;
        }

        public bool LabelExitist(string label)
        {
            for (int i = 0; i != LabelArray.Count; i++)
            {
                if (LabelArray[i] == label)
                {
                    return true;
                }
            }

            return false;
        }

        public void UpdateLabel(int lblIndex, string value)
        {
            LabelArray[lblIndex] = value.Substring(0, 16);
        }

        public string RetLabel(int index)
        {
            return LabelArray[index];
        }

        public int CreateLabel(string value)
        {
            LabelArray.Add(value.Substring(0, 16));
            int index = LabelArray.IndexOf(value);
            return index;
        }

        public List<_GFFStruct> getStructs()
        {
            return StructArray;
        }

        public List<_GFFField> getFields()
        {
            return FieldArray;
        }

        public _GFFField getFieldByLabel(String label)
        {

            foreach(_GFFField field in FieldArray)
            {
                if(field.ReadLabel() == label){
                    return field;
                }
            }

            return new _GFFField();

        }

    }
}
