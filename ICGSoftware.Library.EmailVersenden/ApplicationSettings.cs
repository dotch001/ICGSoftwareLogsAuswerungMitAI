using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json.Nodes;



namespace ICGSoftware.Library.EmailVersenden
{

        public class ApplicationSettingsClass
        {
            public required string[] recipientEmails { get; set; }
            public required string senderEmail { get; set; }
            public required string subject { get; set; }
        }
        public class AuthenticationSettingsClass
        {
            public required string ClientId { get; set; }
            public required string ClientSecret { get; set; }
            public required string TenantId { get; set; }
        }
}
