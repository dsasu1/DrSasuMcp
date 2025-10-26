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
    public class BasicAuthHandler : IAuthenticationHandler
    {
        public AuthType SupportedType => AuthType.Basic;

        public void ApplyAuthentication(HttpRequestMessage request, AuthenticationConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.Username) || string.IsNullOrWhiteSpace(config.Password))
            {
                throw new ArgumentException("Username and password are required for Basic authentication");
            }

            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{config.Username}:{config.Password}")
            );

            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }
    }
}

