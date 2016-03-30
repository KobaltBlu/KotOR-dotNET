using KotOR_Files.AuroraParsers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KotOR_Files
{
    static class Global
    {

        public static Games Game = Games.KOTOR;

        public enum Games
        {
            KOTOR = 1,
            KOTOR2 = 2
        }

        public enum TexturePacks
        {
            TEXTURE_GUI = 0,
            TEXTURE_HIGH = 1,
            TEXTURE_MEDIUM = 2,
            TEXTURE_LOW = 3
        }

    }
}
