using DrSasuMcp.Tools.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrSasuMcp.Tools.API.Validators
{
    public class ResponseTimeValidator : IResponseValidator
    {
        public ValidationType SupportedType => ValidationType.ResponseTime;

        public ValidationResult Validate(HttpResponseResult response, ValidationRule rule)
        {
            var result = new ValidationResult
            {
                ValidationType = ValidationType.ResponseTime,
                Target = "ResponseTime",
                ActualValue = response.ResponseTimeMs,
                ExpectedValue = rule.ExpectedValue
            };

            if (rule.ExpectedValue == null)
            {
                result.IsValid = false;
                result.Message = "Expected value is required for response time validation";
                return result;
            }

            var expectedTime = Convert.ToInt64(rule.ExpectedValue);
            var actualTime = response.ResponseTimeMs;

            result.IsValid = rule.Operator.ToLowerInvariant() switch
            {
                "lessthan" => actualTime < expectedTime,
                "lessthanorequal" => actualTime <= expectedTime,
                "greaterthan" => actualTime > expectedTime,
                "greaterthanorequal" => actualTime >= expectedTime,
                "equals" => actualTime == expectedTime,
                _ => throw new ArgumentException($"Unsupported operator '{rule.Operator}' for response time validation")
            };

            result.Message = result.IsValid
                ? $"Response time {actualTime}ms is {rule.Operator} {expectedTime}ms"
                : $"Response time {actualTime}ms is not {rule.Operator} {expectedTime}ms";

            return result;
        }
    }
}

