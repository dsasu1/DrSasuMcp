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
        public HttpClient CreateClient(int timeoutSeconds = 30, bool followRedirects = true, bool validateSsl = true)
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = followRedirects,
                MaxAutomaticRedirections = 10,
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
    }
}

