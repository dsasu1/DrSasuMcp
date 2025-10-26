using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrSasuMcp.Tools.API.Models
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? ActualValue { get; set; }
        public object? ExpectedValue { get; set; }
        public ValidationType ValidationType { get; set; }
        public string? Target { get; set; }
    }
}

