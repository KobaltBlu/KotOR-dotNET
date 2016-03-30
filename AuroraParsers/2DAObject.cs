using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Data;

namespace KotOR_Files.AuroraParsers
{
    class _2DAObject
    {

        private AuroraFile file;
        private BinaryReader Reader;

        private DataTable data;

        public _2DAObject(AuroraFile file)
        {
            this.file = file;
            data = new DataTable();
        }

        public void Read()
        {

            file.Open();
            Reader = file.getReader();

            char[] fileType = Reader.ReadChars(4);
            char[] fileVersion = Reader.ReadChars(4);

            Reader.BaseStream.Position += 1; //10 = Newline (Skip)

            char[] delimiterChars = { ' ', '\t' };

            string str = "";
            char ch;
            while ((int)(ch = Reader.ReadChar()) != 0)
                str = str + ch;
            DataColumn column = new DataColumn();
            //DataRow row;
            column.DataType = System.Type.GetType("System.Int32");
            column.ColumnName = "(Row Label)";
            column.ReadOnly = true;
            column.Unique = true;
            // Add the Column to the DataColumnCollection.
            data.Columns.Add(column);

            string[] columns = str.Split(delimiterChars);
            foreach(string name in columns)
            {
                column = new DataColumn();
                column.DataType = System.Type.GetType("System.String");
                column.ColumnName = name;
                column.ReadOnly = true;
                column.Unique = false;
                // Add the Column to the DataColumnCollection.
                data.Columns.Add(column);
            }

            UInt32 columnCount = (UInt32)columns.Length - 1;
            UInt32 rowCount = Reader.ReadUInt32();

            string[] rows = new string[rowCount];

            for (int i = 0; i!=rowCount; i++)
            {
                string rowIndex = "";
                char c;

                while ((int)(c = Reader.ReadChar()) != 9)
                    rowIndex = rowIndex + c;

                rows[i] = rowIndex;
            }

            List<int> dataOffsets = new List<int>();
            UInt32 cellCount = columnCount * rowCount;
            uint[] offsets = new uint[cellCount];

            for (int i = 0; i < cellCount; i++)
            {
                offsets[i] = (uint)Reader.ReadUInt16();
            }

            Reader.BaseStream.Position += 2;
            uint dataOffset = (uint)Reader.BaseStream.Position;

            for (int i = 0; i < rowCount; i++)
            {

                DataRow row = data.NewRow();
                row["(Row Label)"] = i;

                for (int j = 0; j < columnCount; j++)
                {
                    uint offset = dataOffset + offsets[i * columnCount + j];

                    try
                    {
                        Reader.BaseStream.Position = offset;
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }

                    string token = "";
                    char c;

                    while((int)(c = Reader.ReadChar()) != 0)
                        token = token + c;

                    if(token == "") 
                        token = "****";
                    
                    Debug.WriteLine(columns[j] + " : " + token);
                    row[columns[j]] = token;
                }

                data.Rows.Add(row);

            }

            file.Close();

        }

        public DataTable getTable()
        {
            return data;
        }

        public DataRowCollection getRows()
        {
            return data.Rows;
        }

    }
}
