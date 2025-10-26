using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrSasuMcp.Tools.API
{
    public partial class APITool
    {
        // Default configuration values
        private const int DefaultTimeoutSeconds = 30;
        private const int MaxTimeoutSeconds = 300;
        private const bool DefaultFollowRedirects = true;
        private const bool DefaultValidateSsl = true;
        private const int DefaultMaxRedirects = 10;
        
        // Environment variable names
        private const string EnvApiDefaultTimeout = "API_DEFAULT_TIMEOUT";
        private const string EnvApiMaxTimeout = "API_MAX_TIMEOUT";
        private const string EnvApiFollowRedirects = "API_FOLLOW_REDIRECTS";
        private const string EnvApiValidateSsl = "API_VALIDATE_SSL";
        private const string EnvApiMaxRedirects = "API_MAX_REDIRECTS";

        // Content type constants
        private const string ContentTypeJson = "application/json";
        private const string ContentTypeXml = "application/xml";
        private const string ContentTypeFormUrlEncoded = "application/x-www-form-urlencoded";
        private const string ContentTypeMultipartFormData = "multipart/form-data";
        private const string ContentTypeTextPlain = "text/plain";

        // Common HTTP methods
        private static readonly string[] SupportedMethods = new[]
        {
            "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS"
        };
    }
}

