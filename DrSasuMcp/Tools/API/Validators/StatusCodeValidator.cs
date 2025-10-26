using DrSasuMcp.Tools.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrSasuMcp.Tools.API.Validators
{
    public class StatusCodeValidator : IResponseValidator
    {
        public ValidationType SupportedType => ValidationType.StatusCode;

        public ValidationResult Validate(HttpResponseResult response, ValidationRule rule)
        {
            var result = new ValidationResult
            {
                ValidationType = ValidationType.StatusCode,
                Target = "StatusCode",
                ActualValue = response.StatusCode,
                ExpectedValue = rule.ExpectedValue
            };

            if (rule.ExpectedValue == null)
            {
                result.IsValid = false;
                result.Message = "Expected value is required for status code validation";
                return result;
            }

            var expectedStatus = Convert.ToInt32(rule.ExpectedValue);
            var actualStatus = response.StatusCode;

            result.IsValid = rule.Operator.ToLowerInvariant() switch
            {
                "equals" => actualStatus == expectedStatus,
                "notequals" => actualStatus != expectedStatus,
                "greaterthan" => actualStatus > expectedStatus,
                "lessthan" => actualStatus < expectedStatus,
                "greaterthanorequal" => actualStatus >= expectedStatus,
                "lessthanorequal" => actualStatus <= expectedStatus,
                _ => throw new ArgumentException($"Unsupported operator '{rule.Operator}' for status code validation")
            };

            result.Message = result.IsValid
                ? $"Status code {actualStatus} matches expected {rule.Operator} {expectedStatus}"
                : $"Status code {actualStatus} does not match expected {rule.Operator} {expectedStatus}";

            return result;
        }
    }
}

