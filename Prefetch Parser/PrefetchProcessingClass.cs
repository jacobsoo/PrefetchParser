using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Data;
using System.Security.Cryptography;

namespace Prefetch_Parser
{
    class PrefetchProcessingClass
    {
        public void ParsePfFile(string filepath, ref PrefetchInfoClass pf)
        {
            byte[] array = new byte[4];
            byte[] timearray = new byte[8];
            BinaryReader binaryReader = new BinaryReader(File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            binaryReader.BaseStream.Position = 0x4C;
            binaryReader.Read(array, 0, 4);
            pf.PathHash = bytestostring_littleendian(array);
            binaryReader.BaseStream.Position = 0x64;
            binaryReader.Read(array, 0, 4);
            int pathoffsets = BitConverter.ToInt32(array, 0);
            binaryReader.BaseStream.Position = 0x68;
            binaryReader.Read(array, 0, 4);
            int size = BitConverter.ToInt32(array, 0);
            binaryReader.BaseStream.Position = 0x6C;
            binaryReader.Read(array, 0, 4);
            int volInfoOffset = BitConverter.ToInt32(array, 0);
            if( pf.IsWin8 ){
                binaryReader.BaseStream.Position = 0xD0;
            }else if (pf.IsVista){
                binaryReader.BaseStream.Position = 0x98;
            }else{
                binaryReader.BaseStream.Position = 0x90;
            }
            binaryReader.Read(array, 0, 4);
            int iNumExecuted = BitConverter.ToInt32(array, 0);
            pf.NumTimesExecuted = iNumExecuted;
            if (pf.IsVista || pf.IsWin8){
                binaryReader.BaseStream.Position = 0x80;
            }else{
                binaryReader.BaseStream.Position = 0x78;
            }
            binaryReader.Read(timearray, 0, 8);
            DateTime dateTime = DateTime.FromFileTimeUtc(BitConverter.ToInt64(timearray, 0));
            pf.LastRun = dateTime;
            if (pf.IsWin8){
                // Now checking for for the last 8 timestamps!
                binaryReader.Read(timearray, 0, 8);
                dateTime = DateTime.FromFileTimeUtc(BitConverter.ToInt64(timearray, 0));
                pf.LastRun2 = dateTime;
                binaryReader.Read(timearray, 0, 8);
                dateTime = DateTime.FromFileTimeUtc(BitConverter.ToInt64(timearray, 0));
                pf.LastRun3 = dateTime;
                binaryReader.Read(timearray, 0, 8);
                dateTime = DateTime.FromFileTimeUtc(BitConverter.ToInt64(timearray, 0));
                pf.LastRun4 = dateTime;
                binaryReader.Read(timearray, 0, 8);
                dateTime = DateTime.FromFileTimeUtc(BitConverter.ToInt64(timearray, 0));
                pf.LastRun5 = dateTime;
                binaryReader.Read(timearray, 0, 8);
                dateTime = DateTime.FromFileTimeUtc(BitConverter.ToInt64(timearray, 0));
                pf.LastRun6 = dateTime;
                binaryReader.Read(timearray, 0, 8);
                dateTime = DateTime.FromFileTimeUtc(BitConverter.ToInt64(timearray, 0));
                pf.LastRun7 = dateTime;
                binaryReader.Read(timearray, 0, 8);
                dateTime = DateTime.FromFileTimeUtc(BitConverter.ToInt64(timearray, 0));
                pf.LastRun8 = dateTime;
            }
            binaryReader.BaseStream.Position = pathoffsets;
            ReadPaths(filepath, pathoffsets, size, ref pf);
            binaryReader.BaseStream.Position = volInfoOffset;
            binaryReader.Read(array, 0, 4);
            int iLen = BitConverter.ToInt32(array, 0);

            long endpos = volInfoOffset + iLen;
            int iBug = 0;
            int iPosition = volInfoOffset + 4;
            while (iLen > 0 && endpos > iPosition){
                // Jump backwards 4 bytes
                iPosition -= 0x4;
                binaryReader.BaseStream.Position -= 0x4;
                PrefetchVolInfoClass vi = new PrefetchVolInfoClass();
                binaryReader.Read(array, 0, 4);
                int iVolTemp = BitConverter.ToInt32(array, 0);
                vi.VolumePathOffset = iVolTemp;
                
                binaryReader.Read(array, 0, 4);
                iVolTemp = BitConverter.ToInt32(array, 0);
                vi.VolumePathLength = iVolTemp;

                binaryReader.Read(timearray, 0, 8);
                dateTime = DateTime.FromFileTimeUtc(BitConverter.ToInt64(timearray, 0));
                vi.CreatedDate = dateTime;                
                
                binaryReader.Read(array, 0, 4);
                string szSerial = bytestostring_littleendian(array);
                vi.Serial = szSerial;

                binaryReader.Read(array, 0, 4);
                iVolTemp = BitConverter.ToInt32(array, 0);
                vi.Blob1Offset = iVolTemp;

                binaryReader.Read(array, 0, 4);
                iVolTemp = BitConverter.ToInt32(array, 0);
                vi.Blob1Length = iVolTemp;

                binaryReader.Read(array, 0, 4);
                iVolTemp = BitConverter.ToInt32(array, 0);
                vi.FolderPathOffset = iVolTemp;

                binaryReader.Read(array, 0, 4);
                iVolTemp = BitConverter.ToInt32(array, 0);
                vi.NumFolderPaths = iVolTemp;

                binaryReader.Read(array, 0, 4);
                iVolTemp = BitConverter.ToInt32(array, 0);
                vi.Unknown = iVolTemp;

                pf.VolumeInfo[iBug] = vi;
                if (pf.IsVista || pf.IsWin8){
                    binaryReader.BaseStream.Position += 64;
                }
                binaryReader.Read(array, 0, 4);
                iLen = BitConverter.ToInt32(array, 0);
                if (++iBug >= 16){
                    //break while, may happen for invalid file!!
                    break;
                }
                iPosition = (int)binaryReader.BaseStream.Position;
            }
            binaryReader.Close();
            int gg = pf.VolumeInfo.Length;
            for(int j=0; j<pf.VolumeInfo.Count(); j++){
                if(pf.VolumeInfo[j]!=null)
                    pf.VolumeInfo[j].ParseInfo(volInfoOffset, filepath);
            }
            pf.PhysicalPath = FindPhysicalPath(filepath, ref pf);
            if (pf.PhysicalPath!=null){
                if (pf.Equals("NTOSBOOT-B00DFAAD.pf"))
                {
                    pf.CalcHash = 0xB00DFAAD;
                }
                else
                {
                    pf.CalcHash = (pf.IsVista || pf.IsWin8) ? GenerateVistaHash(pf.PhysicalPath) : GenerateXpHash(pf.PhysicalPath);
                }
            }
            binaryReader.Close();
        }

        // Same for Vista, 7 & 8
        public long GenerateVistaHash(string str) {
            long ret = 314159;
            int len = str.Length;
            for (int i = 0; i < len; ++i) {
                char c = str[i];
                int num = c;
                if (num > 255)
                    ret = 37 * ((37 * ret) + (num / 256)) + (num % 256);
                else
                    ret = 37 * ((37 * ret) + num);
            }
            return ret;
        }

        // XP Hashing function is : g(K)=(314159269×K) mod 1000000007
        public long GenerateXpHash(string str)
        {
            long ret = 0;
            int len = str.Length;
            for (int i = 0; i < len; ++i) {
                char c = str[i];
                int num = c;
                if (num > 255)
                    ret = 37 * ((37 * ret) + (num / 256)) + (num % 256);
                else
                    ret = 37 * ((37 * ret) + num);
            }
            ret *= 314159269;
            if (ret < 0)
                ret *= -1;
            ret %= 1000000007;
            return ret;
        }

        public string FindPhysicalPath(string filepath, ref PrefetchInfoClass pf)
        {
            int pos = FindLastDash(Path.GetFileName(filepath));
            if (pos > 0){
                string[] szName = Path.GetFileName(filepath).Split('-');
                pf.NameWithoutHash = szName[0];
                int hoho = pf.FilesAccessed.Count();
                for (int g = 0; g<pf.FilesAccessed.Length; g++){
                    if (pf.FilesAccessed[g].IndexOf(pf.NameWithoutHash) > 0)
                    {
                        return pf.FilesAccessed[g];
                    }
                }
            }
            return "";
        }
        
        int FindLastDash(string str) {
            int ret = -1;
            int pos = str.IndexOf("-");
            while (pos >= 0) {
                ret = pos;
                pos = str.IndexOf("-", pos + 1);
            }
            return ret;
        }

        public void ReadPaths(string filepath, int iOffset, int size, ref PrefetchInfoClass pf) {
            int sizeread = 0;
            List<string> szFileAccessed = new List<string>();
            List<string> szActualFileAccessed = new List<string>();
            byte[] buffer = new byte[260];
            string szTemp = "";
            Stream s = new FileStream(filepath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            while (new FileInfo(filepath).Length != 0 && sizeread<size){
                s.Seek(iOffset, SeekOrigin.Begin);
                s.Read(buffer, 0, 260);
                sizeread += buffer.Length;
                szTemp += Encoding.Unicode.GetString(buffer).TrimEnd('\0');
                iOffset += 260;
            }
            szFileAccessed = szTemp.Split(new char[] {'\0'}, StringSplitOptions.RemoveEmptyEntries).ToList();
            int i = szFileAccessed.Count;
            for(int j=0; j<i-1; j++){
                if (szFileAccessed[j].Length>27)
                {
                    szActualFileAccessed.Add(szFileAccessed[j]);
                }
            }
            pf.FilesAccessed = szActualFileAccessed.ToArray();
            s.Close();
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

        public bool IsPrefetchFile(string filepath)
        {
            bool bFlag = false;
            string extension = Path.GetExtension(filepath);
            if( !(Directory.Exists(filepath)) && extension==".pf" )
            {
                bFlag = true;
            }else{
                bFlag = false;
            }
            return bFlag;
        }

        public bool CheckHeader(string filepath, ref bool isVista, ref bool isWin8) {
            int version, signature;
            byte[] array = new byte[4];
            BinaryReader binaryReader = new BinaryReader(File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.Read));
            try{
                binaryReader.BaseStream.Position = 0L;
                binaryReader.Read(array, 0, 4);
                version = BitConverter.ToInt32(array, 0);
                binaryReader.BaseStream.Position = 4L;
                binaryReader.Read(array, 0, 4);
                signature = BitConverter.ToInt32(array, 0);
                isVista = (version == 0x00000017);
                isWin8 = (version == 0x0000001A);
            }finally{
                binaryReader.Close();
            }
            // 17 is vista & 7, 11 is XP, 1A is 8
            return (version == 0x00000011 || version == 0x00000017 || version == 0x0000001A) && signature == 0x41434353; // SCCA or 1094927187
        }
    }
}
