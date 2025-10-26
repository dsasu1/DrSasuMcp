using DrSasuMcp.Tools.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrSasuMcp.Tools.API.Validators
{
    public class BodyContainsValidator : IResponseValidator
    {
        public ValidationType SupportedType => ValidationType.BodyContains;

        public ValidationResult Validate(HttpResponseResult response, ValidationRule rule)
        {
            var result = new ValidationResult
            {
                ValidationType = ValidationType.BodyContains,
                Target = "Body",
                ExpectedValue = rule.ExpectedValue
            };

            if (rule.ExpectedValue == null)
            {
                result.IsValid = false;
                result.Message = "Expected value is required for body contains validation";
                return result;
            }

            var body = response.Body ?? string.Empty;
            var expectedValue = rule.ExpectedValue.ToString() ?? string.Empty;

            result.IsValid = body.Contains(expectedValue);
            result.Message = result.IsValid
                ? $"Body contains '{expectedValue}'"
                : $"Body does not contain '{expectedValue}'";

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

