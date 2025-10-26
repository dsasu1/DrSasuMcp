using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrSasuMcp.Tools.API.Models
{
    public class TestSuiteConfig
    {
        public string Name { get; set; } = string.Empty;
        public string? BaseUrl { get; set; }
        public List<TestConfig> Tests { get; set; } = new();
        public bool StopOnFailure { get; set; } = false;
    }

    public class TestConfig
    {
        public string Name { get; set; } = string.Empty;
        public string Method { get; set; } = "GET";
        public string Path { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? Body { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
        public Dictionary<string, string>? QueryParameters { get; set; }
        public AuthenticationConfig? Authentication { get; set; }
        public int? ExpectedStatus { get; set; }
        public int? MaxResponseTimeMs { get; set; }
        public List<ValidationRule>? Validations { get; set; }
    }
}

