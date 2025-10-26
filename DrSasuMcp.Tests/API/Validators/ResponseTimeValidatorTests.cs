using DrSasuMcp.Tools.API.Models;
using DrSasuMcp.Tools.API.Validators;
using FluentAssertions;
using System;
using Xunit;

namespace DrSasuMcp.Tests.API.Validators
{
    public class ResponseTimeValidatorTests
    {
        private readonly ResponseTimeValidator _validator;

        public ResponseTimeValidatorTests()
        {
            _validator = new ResponseTimeValidator();
        }

        [Fact]
        public void SupportedType_ShouldReturnResponseTime()
        {
            // Act
            var result = _validator.SupportedType;

            // Assert
            result.Should().Be(ValidationType.ResponseTime);
        }

        [Theory]
        [InlineData(100, "lessthan", 200, true)]
        [InlineData(200, "lessthan", 100, false)]
        [InlineData(100, "lessthanorequal", 100, true)]
        [InlineData(100, "lessthanorequal", 200, true)]
        [InlineData(200, "greaterthan", 100, true)]
        [InlineData(100, "greaterthan", 200, false)]
        [InlineData(100, "greaterthanorequal", 100, true)]
        [InlineData(200, "greaterthanorequal", 100, true)]
        [InlineData(100, "equals", 100, true)]
        [InlineData(100, "equals", 200, false)]
        public void Validate_WithVariousOperators_ShouldReturnCorrectResult(
            long actualTime, string operatorValue, long expectedTime, bool expectedValid)
        {
            // Arrange
            var response = new HttpResponseResult
            {
                ResponseTimeMs = actualTime
            };
            var rule = new ValidationRule
            {
                Type = ValidationType.ResponseTime,
                Operator = operatorValue,
                ExpectedValue = expectedTime
            };

            // Act
            var result = _validator.Validate(response, rule);

            // Assert
            result.IsValid.Should().Be(expectedValid);
            result.ValidationType.Should().Be(ValidationType.ResponseTime);
            result.ActualValue.Should().Be(actualTime);
            result.ExpectedValue.Should().Be(expectedTime);
            result.Target.Should().Be("ResponseTime");
            result.Message.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void Validate_WithNullExpectedValue_ShouldReturnInvalid()
        {
            // Arrange
            var response = new HttpResponseResult
            {
                ResponseTimeMs = 100
            };
            var rule = new ValidationRule
            {
                Type = ValidationType.ResponseTime,
                Operator = "lessthan",
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
                ResponseTimeMs = 100
            };
            var rule = new ValidationRule
            {
                Type = ValidationType.ResponseTime,
                Operator = "contains",
                ExpectedValue = 100
            };

            // Act
            Action act = () => _validator.Validate(response, rule);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Unsupported operator*");
        }

        [Fact]
        public void Validate_WithZeroResponseTime_ShouldWork()
        {
            // Arrange
            var response = new HttpResponseResult
            {
                ResponseTimeMs = 0
            };
            var rule = new ValidationRule
            {
                Type = ValidationType.ResponseTime,
                Operator = "equals",
                ExpectedValue = 0
            };

            // Act
            var result = _validator.Validate(response, rule);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WithLargeResponseTime_ShouldWork()
        {
            // Arrange
            var response = new HttpResponseResult
            {
                ResponseTimeMs = 30000 // 30 seconds
            };
            var rule = new ValidationRule
            {
                Type = ValidationType.ResponseTime,
                Operator = "greaterthan",
                ExpectedValue = 1000
            };

            // Act
            var result = _validator.Validate(response, rule);

            // Assert
            result.IsValid.Should().BeTrue();
        }
    }
}

