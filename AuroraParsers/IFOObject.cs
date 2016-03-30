using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KotOR_Files.AuroraParsers
{
    public class IFOObject
    {

        AuroraFile file;
        BinaryReader Reader;

        public IFOObject(AuroraFile file)
        {
            this.file = file;
        }

        public void Read()
        {
            file.Open();
            Reader = file.getReader();




            file.Close();
        }

    }
}
