using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrSasuMcp.Tools.API.Models
{
    public class ValidationRule
    {
        public ValidationType Type { get; set; }
        
        /// <summary>
        /// Target for validation (JSONPath expression, header name, etc.)
        /// </summary>
        public string Target { get; set; } = string.Empty;
        
        /// <summary>
        /// Comparison operator: equals, contains, greaterThan, lessThan, greaterThanOrEqual, lessThanOrEqual, exists, notExists, matches
        /// </summary>
        public string Operator { get; set; } = "equals";
        
        /// <summary>
        /// Expected value for comparison
        /// </summary>
        public object? ExpectedValue { get; set; }
        
        /// <summary>
        /// Optional description of this validation
        /// </summary>
        public string? Description { get; set; }
    }
}

