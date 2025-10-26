using DrSasuMcp.Tools.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DrSasuMcp.Tools.API.Authentication
{
    public class CustomAuthHandler : IAuthenticationHandler
    {
        public AuthType SupportedType => AuthType.Custom;

        public void ApplyAuthentication(HttpRequestMessage request, AuthenticationConfig config)
        {
            if (config.CustomHeaders == null || !config.CustomHeaders.Any())
            {
                throw new ArgumentException("Custom headers are required for Custom authentication");
            }

            foreach (var header in config.CustomHeaders)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }
    }
}

