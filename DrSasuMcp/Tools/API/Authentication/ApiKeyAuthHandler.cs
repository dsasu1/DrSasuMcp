using DrSasuMcp.Tools.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DrSasuMcp.Tools.API.Authentication
{
    public class ApiKeyAuthHandler : IAuthenticationHandler
    {
        public AuthType SupportedType => AuthType.ApiKey;

        public void ApplyAuthentication(HttpRequestMessage request, AuthenticationConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.ApiKeyHeader) || string.IsNullOrWhiteSpace(config.ApiKeyValue))
            {
                throw new ArgumentException("API Key header name and value are required for API Key authentication");
            }

            request.Headers.Add(config.ApiKeyHeader, config.ApiKeyValue);
        }
    }
}

