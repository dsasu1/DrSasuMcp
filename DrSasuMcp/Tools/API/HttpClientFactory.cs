using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DrSasuMcp.Tools.API
{
    public class HttpClientFactory : IHttpClientFactory
    {
        private const int DefaultMaxRedirects = 10;
        private const string EnvApiMaxRedirects = "API_MAX_REDIRECTS";
        
        public HttpClient CreateClient(int timeoutSeconds = 30, bool followRedirects = true, bool validateSsl = true)
        {
            // Read max redirects from environment or use default
            var maxRedirects = GetIntFromEnv(EnvApiMaxRedirects, DefaultMaxRedirects);
            
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = followRedirects,
                MaxAutomaticRedirections = maxRedirects,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            // SSL validation
            if (!validateSsl)
            {
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            }

            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds)
            };

            return client;
        }
        
        private static int GetIntFromEnv(string varName, int defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(varName);
            return int.TryParse(value, out var result) ? result : defaultValue;
        }
    }
}

