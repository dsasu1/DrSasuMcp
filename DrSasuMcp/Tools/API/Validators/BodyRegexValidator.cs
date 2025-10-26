using DrSasuMcp.Tools.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DrSasuMcp.Tools.API.Validators
{
    public class BodyRegexValidator : IResponseValidator
    {
        public ValidationType SupportedType => ValidationType.BodyRegex;

        public ValidationResult Validate(HttpResponseResult response, ValidationRule rule)
        {
            var result = new ValidationResult
            {
                ValidationType = ValidationType.BodyRegex,
                Target = "Body",
                ExpectedValue = rule.ExpectedValue
            };

            if (rule.ExpectedValue == null)
            {
                result.IsValid = false;
                result.Message = "Expected regex pattern is required for body regex validation";
                return result;
            }

            var body = response.Body ?? string.Empty;
            var pattern = rule.ExpectedValue.ToString() ?? string.Empty;

            try
            {
                result.IsValid = Regex.IsMatch(body, pattern);
                result.Message = result.IsValid
                    ? $"Body matches regex pattern '{pattern}'"
                    : $"Body does not match regex pattern '{pattern}'";

                if (!result.IsValid && body.Length > 100)
                {
                    result.ActualValue = body.Substring(0, 100) + "...";
                }
                else if (!result.IsValid)
                {
                    result.ActualValue = body;
                }
            }
            catch (ArgumentException ex)
            {
                result.IsValid = false;
                result.Message = $"Invalid regex pattern: {ex.Message}";
            }

            return result;
        }
    }
}

