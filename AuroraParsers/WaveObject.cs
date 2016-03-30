using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace KotOR_Files.AuroraParsers
{
    public class WaveObject
    {

        private byte[] bytes;
        private AudioType audioType = AudioType.Unknown;

        private BinaryReader br;
        private AuroraFile file;

        public int offset = 58;

        public enum AudioType
        {
            Unknown,
            WAVE,
            MP3
        }

        public WaveObject(AuroraFile file)
        {
            this.file = file;
            this.file.Open();
            br = file.getReader();

            try {
                //Check for real WAVE file
                br.BaseStream.Position = 470;
                String riff = new string(br.ReadChars(4));
                if (riff == "RIFF") {
                    offset = 470;
                    audioType = AudioType.WAVE;
                    Debug.WriteLine("Found: " + riff);
                }
                else
                {
                    Debug.WriteLine(riff);
                }
            }catch(Exception ex)
            {
                audioType = AudioType.WAVE;
                Debug.WriteLine(ex.ToString());
            }

            br.BaseStream.Position = 32;
            String data = new string(br.ReadChars(4));
            if (data == "data")
            {
                offset = 32;
                //audioType = AudioType.WAVE;
                Debug.WriteLine("Found: " + data);
            }

            //Check for real MP3 file
            br.BaseStream.Position = 199;
            String lame = new string(br.ReadChars(4));
            if (lame == "LAME")
            {
                offset = 199;
                audioType = AudioType.MP3;
                Debug.WriteLine("Found: "+lame);
            }
            else
            {
                Debug.WriteLine(lame);
            }

            //Check for real MP3 file
            br.BaseStream.Position = 200;
            lame = new string(br.ReadChars(4));
            if (lame == "LAME")
            {
                offset = 200;
                audioType = AudioType.MP3;
                Debug.WriteLine(lame);
            }

            file.Close(); //Close the file because we are done reading data...

        }

        public byte[] getPlayableByteStream()
        {
            file.Open();

            byte[] data;
            using (var br = file.getReader())
            {
                br.BaseStream.Position = offset;
                data = br.ReadBytes((int)br.BaseStream.Length - offset);
            }

            file.Close();

            return data;
        }

        public AudioType getType()
        {
            return audioType;
        }

        public static void PlayInExternalPlayer(AuroraFile file)
        {
            WaveObject audio = new WaveObject(file);

            //byte[] bytes = audio.getPlayableByteStream();

            MemoryStream dataStream = new MemoryStream(audio.getPlayableByteStream());

            switch (audio.getType())
            {
                case WaveObject.AudioType.WAVE:
                    Debug.WriteLine("Playing: WAV");
                    //simpleSound = new SoundPlayer(dataStream);
                    //simpleSound.Play();

                    FileStream fsW = new FileStream("test.wav", FileMode.Create, FileAccess.Write);
                    BinaryWriter bwW = new BinaryWriter(fsW);
                    bwW.Write(audio.getPlayableByteStream());
                    bwW.Close();
                    fsW.Close();

                    System.Diagnostics.Process.Start("test.wav");

                    break;
                case WaveObject.AudioType.MP3:
                default:
                    Debug.WriteLine("Playing: MP3");

                    FileStream fs = new FileStream("test.mp3", FileMode.Create, FileAccess.Write);
                    BinaryWriter bw = new BinaryWriter(fs);
                    bw.Write(audio.getPlayableByteStream());
                    bw.Close();
                    fs.Close();

                    System.Diagnostics.Process.Start("test.mp3");

                    //Mp3FileReader mp3 = new Mp3FileReader(dataStream);
                    //WaveStream pcm = WaveFormatConversionStream.CreatePcmStream(mp3);
                    //simpleSound = new SoundPlayer(pcm);
                    //File.WriteAllBytes("test.mp3", audio.getPlayableByteStream());
                    break;
            }
        }

    }
}
