using DrSasuMcp.API.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DrSasuMcp.API.API.Authentication
{
    public interface IAuthenticationHandler
    {
        AuthType SupportedType { get; }
        void ApplyAuthentication(HttpRequestMessage request, AuthenticationConfig config);
    }
}

