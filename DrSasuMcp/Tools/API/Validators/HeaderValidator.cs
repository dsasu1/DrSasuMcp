using DrSasuMcp.Tools.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrSasuMcp.Tools.API.Validators
{
    public class HeaderValidator : IResponseValidator
    {
        public ValidationType SupportedType => ValidationType.Header;

        public ValidationResult Validate(HttpResponseResult response, ValidationRule rule)
        {
            var result = new ValidationResult
            {
                ValidationType = ValidationType.Header,
                Target = rule.Target,
                ExpectedValue = rule.ExpectedValue
            };

            if (string.IsNullOrWhiteSpace(rule.Target))
            {
                result.IsValid = false;
                result.Message = "Header name (target) is required";
                return result;
            }

            var headerExists = response.Headers.TryGetValue(rule.Target, out var headerValue);
            result.ActualValue = headerValue;

            result.IsValid = rule.Operator.ToLowerInvariant() switch
            {
                "exists" => headerExists,
                "notexists" => !headerExists,
                "equals" => headerExists && headerValue == rule.ExpectedValue?.ToString(),
                "contains" => headerExists && headerValue?.Contains(rule.ExpectedValue?.ToString() ?? "") == true,
                "notequals" => headerExists && headerValue != rule.ExpectedValue?.ToString(),
                _ => throw new ArgumentException($"Unsupported operator '{rule.Operator}' for header validation")
            };

            result.Message = result.IsValid
                ? $"Header '{rule.Target}' validation passed"
                : $"Header '{rule.Target}' validation failed: {(headerExists ? $"value is '{headerValue}'" : "header not found")}";

            return result;
        }
    }
}

