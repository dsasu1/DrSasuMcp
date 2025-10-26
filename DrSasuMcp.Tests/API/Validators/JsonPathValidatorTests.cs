using DrSasuMcp.Tools.API.Models;
using DrSasuMcp.Tools.API.Validators;
using FluentAssertions;
using System;
using Xunit;

namespace DrSasuMcp.Tests.API.Validators
{
    public class JsonPathValidatorTests
    {
        private readonly JsonPathValidator _validator;

        public JsonPathValidatorTests()
        {
            _validator = new JsonPathValidator();
        }

        [Fact]
        public void SupportedType_ShouldReturnJsonPath()
        {
            // Act
            var result = _validator.SupportedType;

            // Assert
            result.Should().Be(ValidationType.JsonPath);
        }

        [Fact]
        public void Validate_WithSimplePropertyExists_ShouldReturnValid()
        {
            // Arrange
            var response = new HttpResponseResult
            {
                Body = @"{""name"": ""John Doe"", ""age"": 30}"
            };
            var rule = new ValidationRule
            {
                Type = ValidationType.JsonPath,
                Target = "$.name",
                Operator = "exists"
            };

            // Act
            var result = _validator.Validate(response, rule);

            // Assert
            result.IsValid.Should().BeTrue();
            result.ActualValue.Should().Be("John Doe");
        }

        [Fact]
        public void Validate_WithSimplePropertyEquals_ShouldReturnValid()
        {
            // Arrange
            var response = new HttpResponseResult
            {
                Body = @"{""name"": ""John Doe"", ""age"": 30}"
            };
            var rule = new ValidationRule
            {
                Type = ValidationType.JsonPath,
                Target = "$.name",
                Operator = "equals",
                ExpectedValue = "John Doe"
            };

            // Act
            var result = _validator.Validate(response, rule);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WithNestedProperty_ShouldReturnValid()
        {
            // Arrange
            var response = new HttpResponseResult
            {
                Body = @"{""user"": {""name"": ""John"", ""email"": ""john@example.com""}}"
            };
            var rule = new ValidationRule
            {
                Type = ValidationType.JsonPath,
                Target = "$.user.email",
                Operator = "equals",
                ExpectedValue = "john@example.com"
            };

            // Act
            var result = _validator.Validate(response, rule);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WithArrayIndex_ShouldReturnValid()
        {
            // Arrange
            var response = new HttpResponseResult
            {
                Body = @"{""users"": [{""name"": ""John""}, {""name"": ""Jane""}]}"
            };
            var rule = new ValidationRule
            {
                Type = ValidationType.JsonPath,
                Target = "$.users[0].name",
                Operator = "equals",
                ExpectedValue = "John"
            };

            // Act
            var result = _validator.Validate(response, rule);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WithArrayLength_ShouldReturnValid()
        {
            // Arrange
            var response = new HttpResponseResult
            {
                Body = @"{""users"": [1, 2, 3, 4, 5]}"
            };
            var rule = new ValidationRule
            {
                Type = ValidationType.JsonPath,
                Target = "$.users.length",
                Operator = "equals",
                ExpectedValue = 5
            };

            // Act
            var result = _validator.Validate(response, rule);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WithNumericComparison_ShouldReturnValid()
        {
            // Arrange
            var response = new HttpResponseResult
            {
                Body = @"{""age"": 30, ""score"": 95.5}"
            };
            var rule = new ValidationRule
            {
                Type = ValidationType.JsonPath,
                Target = "$.age",
                Operator = "greaterthan",
                ExpectedValue = 25
            };

            // Act
            var result = _validator.Validate(response, rule);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WithContainsOperator_ShouldReturnValid()
        {
            // Arrange
            var response = new HttpResponseResult
            {
                Body = @"{""message"": ""Hello World""}"
            };
            var rule = new ValidationRule
            {
                Type = ValidationType.JsonPath,
                Target = "$.message",
                Operator = "contains",
                ExpectedValue = "World"
            };

            // Act
            var result = _validator.Validate(response, rule);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WithNonExistentProperty_ShouldReturnInvalid()
        {
            // Arrange
            var response = new HttpResponseResult
            {
                Body = @"{""name"": ""John""}"
            };
            var rule = new ValidationRule
            {
                Type = ValidationType.JsonPath,
                Target = "$.age",
                Operator = "exists"
            };

            // Act
            var result = _validator.Validate(response, rule);

            // Assert
            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_WithNotExistsOperator_ShouldReturnValid()
        {
            // Arrange
            var response = new HttpResponseResult
            {
                Body = @"{""name"": ""John""}"
            };
            var rule = new ValidationRule
            {
                Type = ValidationType.JsonPath,
                Target = "$.age",
                Operator = "notexists"
            };

            // Act
            var result = _validator.Validate(response, rule);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WithInvalidJson_ShouldReturnInvalid()
        {
            // Arrange
            var response = new HttpResponseResult
            {
                Body = @"invalid json"
            };
            var rule = new ValidationRule
            {
                Type = ValidationType.JsonPath,
                Target = "$.name",
                Operator = "exists"
            };

            // Act
            var result = _validator.Validate(response, rule);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Message.Should().Contain("Failed to parse JSON");
        }

        [Fact]
        public void Validate_WithEmptyTarget_ShouldReturnInvalid()
        {
            // Arrange
            var response = new HttpResponseResult
            {
                Body = @"{""name"": ""John""}"
            };
            var rule = new ValidationRule
            {
                Type = ValidationType.JsonPath,
                Target = "",
                Operator = "exists"
            };

            // Act
            var result = _validator.Validate(response, rule);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Message.Should().Contain("JSONPath expression (target) is required");
        }

        [Fact]
        public void Validate_WithUnsupportedOperator_ShouldThrowArgumentException()
        {
            // Arrange
            var response = new HttpResponseResult
            {
                Body = @"{""name"": ""John""}"
            };
            var rule = new ValidationRule
            {
                Type = ValidationType.JsonPath,
                Target = "$.name",
                Operator = "invalidoperator",
                ExpectedValue = "John"
            };

            // Act
            var result = _validator.Validate(response, rule);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Message.Should().Contain("Unsupported operator");
        }

        [Fact]
        public void Validate_WithBooleanValue_ShouldWork()
        {
            // Arrange
            var response = new HttpResponseResult
            {
                Body = @"{""isActive"": true}"
            };
            var rule = new ValidationRule
            {
                Type = ValidationType.JsonPath,
                Target = "$.isActive",
                Operator = "equals",
                ExpectedValue = true
            };

            // Act
            var result = _validator.Validate(response, rule);

            // Assert
            result.IsValid.Should().BeTrue();
        }
    }
}

