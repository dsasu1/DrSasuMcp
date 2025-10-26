using DrSasuMcp.Tools.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrSasuMcp.Tools.API.Validators
{
    public class BodyEqualsValidator : IResponseValidator
    {
        public ValidationType SupportedType => ValidationType.BodyEquals;

        public ValidationResult Validate(HttpResponseResult response, ValidationRule rule)
        {
            var result = new ValidationResult
            {
                ValidationType = ValidationType.BodyEquals,
                Target = "Body",
                ExpectedValue = rule.ExpectedValue
            };

            if (rule.ExpectedValue == null)
            {
                result.IsValid = false;
                result.Message = "Expected value is required for body equals validation";
                return result;
            }

            var body = response.Body ?? string.Empty;
            var expectedValue = rule.ExpectedValue.ToString() ?? string.Empty;

            result.IsValid = body == expectedValue;
            result.Message = result.IsValid
                ? "Body matches expected value"
                : "Body does not match expected value";

            if (!result.IsValid && body.Length > 100)
            {
                result.ActualValue = body.Substring(0, 100) + "...";
            }
            else if (!result.IsValid)
            {
                result.ActualValue = body;
            }

            return result;
        }
    }
}

