using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrSasuMcp.Tools.API.Models
{
    public class AuthenticationConfig
    {
        public AuthType Type { get; set; }
        
        /// <summary>
        /// Bearer token for Bearer authentication
        /// </summary>
        public string? Token { get; set; }
        
        /// <summary>
        /// Username for Basic authentication
        /// </summary>
        public string? Username { get; set; }
        
        /// <summary>
        /// Password for Basic authentication
        /// </summary>
        public string? Password { get; set; }
        
        /// <summary>
        /// Header name for API Key authentication (e.g., "X-API-Key")
        /// </summary>
        public string? ApiKeyHeader { get; set; }
        
        /// <summary>
        /// API Key value
        /// </summary>
        public string? ApiKeyValue { get; set; }
        
        /// <summary>
        /// Custom headers for Custom authentication
        /// </summary>
        public Dictionary<string, string>? CustomHeaders { get; set; }
    }
}

