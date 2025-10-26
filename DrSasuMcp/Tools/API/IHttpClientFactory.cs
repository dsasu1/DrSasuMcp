using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DrSasuMcp.Tools.API
{
    public interface IHttpClientFactory
    {
        HttpClient CreateClient(int timeoutSeconds = 30, bool followRedirects = true, bool validateSsl = true);
    }
}

