using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICGSoftware.LogAuswertung
{

    class ApplicationSettings
    {
        public string ApiKey { get; set; }
        public string Question { get; set; }
        public string startTerm { get; set; }
        public string[] inputFolderPaths { get; set; }
        public string outputFolderPath { get; set; }
        public bool inform { get; set; }
        public bool AskGPT { get; set; }
        public int maxSizeInKB { get; set; }
    }

}