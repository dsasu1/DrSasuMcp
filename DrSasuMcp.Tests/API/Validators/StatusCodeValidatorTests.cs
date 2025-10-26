using DrSasuMcp.Tools.API.Models;
using DrSasuMcp.Tools.API.Validators;
using FluentAssertions;
using System;
using Xunit;

namespace DrSasuMcp.Tests.API.Validators
{
    public class StatusCodeValidatorTests
    {
        private readonly StatusCodeValidator _validator;

        public StatusCodeValidatorTests()
        {
            _validator = new StatusCodeValidator();
        }

        [Fact]
        public void SupportedType_ShouldReturnStatusCode()
        {
            // Act
            var result = _validator.SupportedType;

            // Assert
            result.Should().Be(ValidationType.StatusCode);
        }

        [Theory]
        [InlineData(200, "equals", 200, true)]
        [InlineData(200, "equals", 404, false)]
        [InlineData(200, "notequals", 404, true)]
        [InlineData(200, "notequals", 200, false)]
        [InlineData(200, "greaterthan", 199, true)]
        [InlineData(200, "greaterthan", 200, false)]
        [InlineData(200, "lessthan", 201, true)]
        [InlineData(200, "lessthan", 200, false)]
        [InlineData(200, "greaterthanorequal", 200, true)]
        [InlineData(200, "greaterthanorequal", 199, true)]
        [InlineData(200, "lessthanorequal", 200, true)]
        [InlineData(200, "lessthanorequal", 201, true)]
        public void Validate_WithVariousOperators_ShouldReturnCorrectResult(
            int actualStatus, string operatorValue, int expectedStatus, bool expectedValid)
        {
            // Arrange
            var response = new HttpResponseResult
            {
                StatusCode = actualStatus
            };
            var rule = new ValidationRule
            {
                Type = ValidationType.StatusCode,
                Operator = operatorValue,
                ExpectedValue = expectedStatus
            };

            // Act
            var result = _validator.Validate(response, rule);

            // Assert
            result.IsValid.Should().Be(expectedValid);
            result.ValidationType.Should().Be(ValidationType.StatusCode);
            result.ActualValue.Should().Be(actualStatus);
            result.ExpectedValue.Should().Be(expectedStatus);
            result.Target.Should().Be("StatusCode");
            result.Message.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void Validate_WithNullExpectedValue_ShouldReturnInvalid()
        {
            // Arrange
            var response = new HttpResponseResult
            {
                StatusCode = 200
            };
            var rule = new ValidationRule
            {
                Type = ValidationType.StatusCode,
                Operator = "equals",
                ExpectedValue = null
            };

            // Act
            var result = _validator.Validate(response, rule);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Message.Should().Contain("Expected value is required");
        }

        [Fact]
        public void Validate_WithUnsupportedOperator_ShouldThrowArgumentException()
        {
            // Arrange
            var response = new HttpResponseResult
            {
                StatusCode = 200
            };
            var rule = new ValidationRule
            {
                Type = ValidationType.StatusCode,
                Operator = "invalidoperator",
                ExpectedValue = 200
            };

            // Act
            Action act = () => _validator.Validate(response, rule);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Unsupported operator*");
        }

        [Theory]
        [InlineData("EQUALS")]
        [InlineData("Equals")]
        [InlineData("equals")]
        public void Validate_WithDifferentCasing_ShouldWork(string operatorValue)
        {
            // Arrange
            var response = new HttpResponseResult
            {
                StatusCode = 200
            };
            var rule = new ValidationRule
            {
                Type = ValidationType.StatusCode,
                Operator = operatorValue,
                ExpectedValue = 200
            };

            // Act
            var result = _validator.Validate(response, rule);

            // Assert
            result.IsValid.Should().BeTrue();
        }
    }
}

