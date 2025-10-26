using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrSasuMcp.Tools.API.Models
{
    public class TestResult
    {
        public bool TestPassed { get; set; }
        public int TotalValidations { get; set; }
        public int PassedValidations { get; set; }
        public int FailedValidations { get; set; }
        public long ResponseTimeMs { get; set; }
        public List<ValidationResult> ValidationResults { get; set; } = new();
        public HttpResponseResult? Response { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

