using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Prefetch_Parser
{
    class PrefetchVolInfoClass
    {
        public int VolumePathOffset;
        public int VolumePathLength;
        public DateTime CreatedDate;
        public string Serial;
        public int Blob1Offset;
        public int Blob1Length;
        public int FolderPathOffset;
        public int NumFolderPaths;
        public int Unknown;
        public string[] FolderPaths = { };

        public void ParseInfo(long pos, string filePath) {
            byte[] array = new byte[100000];
            BinaryReader binaryReader = new BinaryReader(File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read));
            try
            {
                binaryReader.BaseStream.Position = pos + VolumePathOffset;
                binaryReader.Read(array, 0, VolumePathLength*2);
                string name = Encoding.Unicode.GetString(array).TrimEnd('\0'); //bytestostring_littleendian(array);
                binaryReader.BaseStream.Position = pos + FolderPathOffset;
                List<string> iFolderPaths = new List<string>();
                for (int i = 0; i < NumFolderPaths; ++i)
                {
                    binaryReader.Read(array, 0, 2);
                    int len = BitConverter.ToInt16(array, 0);
                    binaryReader.Read(array, 0, len*2);
                    string szPath = Encoding.Unicode.GetString(array).TrimEnd('\0');
                    if (szPath != "")
                        iFolderPaths.Add(szPath);
                    binaryReader.Read(array, 0, 2);
                }
                FolderPaths = iFolderPaths.ToArray();
            }
            finally
            {
                binaryReader.Close();
            }
        }

        public string bytestostring_littleendian(byte[] toconvert)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = toconvert.Length - 1; i >= 0; i--)
            {
                stringBuilder.Append(toconvert[i].ToString("X2"));
            }
            return stringBuilder.ToString();
        }
    }
}
