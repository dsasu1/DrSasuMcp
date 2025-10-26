using DrSasuMcp.Tools.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DrSasuMcp.Tools.API.Validators
{
    public class JsonPathValidator : IResponseValidator
    {
        public ValidationType SupportedType => ValidationType.JsonPath;

        public ValidationResult Validate(HttpResponseResult response, ValidationRule rule)
        {
            var result = new ValidationResult
            {
                ValidationType = ValidationType.JsonPath,
                Target = rule.Target,
                ExpectedValue = rule.ExpectedValue
            };

            if (string.IsNullOrWhiteSpace(rule.Target))
            {
                result.IsValid = false;
                result.Message = "JSONPath expression (target) is required";
                return result;
            }

            try
            {
                // Parse the JSON response
                var jsonDoc = JsonDocument.Parse(response.Body);
                
                // Simple JSONPath implementation (supports basic paths like $.field, $.array[0], $.nested.field)
                var value = EvaluateJsonPath(jsonDoc.RootElement, rule.Target);
                result.ActualValue = value;

                result.IsValid = rule.Operator.ToLowerInvariant() switch
                {
                    "exists" => value != null,
                    "notexists" => value == null,
                    "equals" => CompareValues(value, rule.ExpectedValue),
                    "notequals" => !CompareValues(value, rule.ExpectedValue),
                    "contains" => value?.ToString()?.Contains(rule.ExpectedValue?.ToString() ?? "") == true,
                    "greaterthan" => CompareNumeric(value, rule.ExpectedValue, (a, b) => a > b),
                    "lessthan" => CompareNumeric(value, rule.ExpectedValue, (a, b) => a < b),
                    "greaterthanorequal" => CompareNumeric(value, rule.ExpectedValue, (a, b) => a >= b),
                    "lessthanorequal" => CompareNumeric(value, rule.ExpectedValue, (a, b) => a <= b),
                    _ => throw new ArgumentException($"Unsupported operator '{rule.Operator}' for JSONPath validation")
                };

                result.Message = result.IsValid
                    ? $"JSONPath '{rule.Target}' validation passed"
                    : $"JSONPath '{rule.Target}' validation failed: actual value is '{value}'";
            }
            catch (JsonException ex)
            {
                result.IsValid = false;
                result.Message = $"Failed to parse JSON: {ex.Message}";
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Message = $"JSONPath validation error: {ex.Message}";
            }

            return result;
        }

        private object? EvaluateJsonPath(JsonElement root, string path)
        {
            // Remove leading $. or $
            path = path.TrimStart('$').TrimStart('.');

            if (string.IsNullOrEmpty(path))
                return JsonElementToObject(root);

            var parts = path.Split('.');
            var current = root;

            foreach (var part in parts)
            {
                // Handle array indexing like "array[0]"
                if (part.Contains('[') && part.Contains(']'))
                {
                    var propertyName = part.Substring(0, part.IndexOf('['));
                    var indexStr = part.Substring(part.IndexOf('[') + 1, part.IndexOf(']') - part.IndexOf('[') - 1);
                    
                    if (!string.IsNullOrEmpty(propertyName))
                    {
                        if (current.TryGetProperty(propertyName, out var arrayElement))
                        {
                            current = arrayElement;
                        }
                        else
                        {
                            return null;
                        }
                    }

                    if (int.TryParse(indexStr, out var index))
                    {
                        if (current.ValueKind == JsonValueKind.Array && index < current.GetArrayLength())
                        {
                            current = current[index];
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                // Handle "length" property for arrays
                else if (part.ToLowerInvariant() == "length" && current.ValueKind == JsonValueKind.Array)
                {
                    return current.GetArrayLength();
                }
                else
                {
                    if (current.TryGetProperty(part, out var property))
                    {
                        current = property;
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            return JsonElementToObject(current);
        }

        private object? JsonElementToObject(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt32(out var intVal) ? intVal : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToList(),
                JsonValueKind.Object => element.ToString(),
                _ => element.ToString()
            };
        }

        private bool CompareValues(object? actual, object? expected)
        {
            if (actual == null && expected == null) return true;
            if (actual == null || expected == null) return false;

            // Try numeric comparison first
            if (IsNumeric(actual) && IsNumeric(expected))
            {
                return Convert.ToDouble(actual) == Convert.ToDouble(expected);
            }

            return actual.ToString() == expected.ToString();
        }

        private bool CompareNumeric(object? actual, object? expected, Func<double, double, bool> comparison)
        {
            if (actual == null || expected == null) return false;
            
            if (!IsNumeric(actual) || !IsNumeric(expected))
            {
                throw new ArgumentException("Both values must be numeric for numeric comparison");
            }

            return comparison(Convert.ToDouble(actual), Convert.ToDouble(expected));
        }

        private bool IsNumeric(object? value)
        {
            return value is int or long or float or double or decimal;
        }
    }
}

