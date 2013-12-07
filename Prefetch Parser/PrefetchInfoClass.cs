using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Prefetch_Parser
{
    class PrefetchInfoClass
    {
        public DateTime LastRun,
            LastRun2,
            LastRun3,
            LastRun4,
            LastRun5,
            LastRun6,
            LastRun7,
            LastRun8;
        public int NumTimesExecuted;
        public string PathHash;
        public int TestHash;
        public long CalcHash;
        public string[] FilesAccessed;
        public bool IsVista,
            IsWin8;
        public PrefetchVolInfoClass[] VolumeInfo = new PrefetchVolInfoClass[10];
        public string FilePath,
            NameWithoutHash,
            PhysicalPath;
    }
}
