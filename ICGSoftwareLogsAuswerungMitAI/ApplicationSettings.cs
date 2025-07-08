using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICGSoftwareLogAuswertung
{

    class ApplicationSettings
    {
        public string ApiKey { get; set; }
        public string Question { get; set; }
        public string startTerm { get; set; }
        public string[] inputFolderPaths { get; set; }
        public bool inform { get; set; }
        public bool AskGPT { get; set; }
    }

}