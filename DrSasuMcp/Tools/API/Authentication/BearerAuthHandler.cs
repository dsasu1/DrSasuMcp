using DrSasuMcp.Tools.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DrSasuMcp.Tools.API.Authentication
{
    public class BearerAuthHandler : IAuthenticationHandler
    {
        public AuthType SupportedType => AuthType.Bearer;

        public void ApplyAuthentication(HttpRequestMessage request, AuthenticationConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.Token))
            {
                throw new ArgumentException("Bearer token is required for Bearer authentication", nameof(config.Token));
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.Token);
        }
    }
}

