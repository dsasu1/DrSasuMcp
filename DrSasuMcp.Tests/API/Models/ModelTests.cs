using DrSasuMcp.Tools.API.Models;
using FluentAssertions;
using Xunit;

namespace DrSasuMcp.Tests.API.Models
{
    public class AuthenticationConfigTests
    {
        [Fact]
        public void AuthenticationConfig_DefaultValues_ShouldBeNull()
        {
            // Act
            var config = new AuthenticationConfig();

            // Assert
            config.Type.Should().Be(AuthType.None);
            config.Token.Should().BeNull();
            config.Username.Should().BeNull();
            config.Password.Should().BeNull();
            config.ApiKeyHeader.Should().BeNull();
            config.ApiKeyValue.Should().BeNull();
            config.CustomHeaders.Should().BeNull();
        }

        [Fact]
        public void AuthenticationConfig_BearerAuth_ShouldSetToken()
        {
            // Act
            var config = new AuthenticationConfig
            {
                Type = AuthType.Bearer,
                Token = "test-token"
            };

            // Assert
            config.Type.Should().Be(AuthType.Bearer);
            config.Token.Should().Be("test-token");
        }

        [Fact]
        public void AuthenticationConfig_BasicAuth_ShouldSetCredentials()
        {
            // Act
            var config = new AuthenticationConfig
            {
                Type = AuthType.Basic,
                Username = "user",
                Password = "pass"
            };

            // Assert
            config.Type.Should().Be(AuthType.Basic);
            config.Username.Should().Be("user");
            config.Password.Should().Be("pass");
        }

        [Fact]
        public void AuthenticationConfig_ApiKeyAuth_ShouldSetApiKey()
        {
            // Act
            var config = new AuthenticationConfig
            {
                Type = AuthType.ApiKey,
                ApiKeyHeader = "X-API-Key",
                ApiKeyValue = "key123"
            };

            // Assert
            config.Type.Should().Be(AuthType.ApiKey);
            config.ApiKeyHeader.Should().Be("X-API-Key");
            config.ApiKeyValue.Should().Be("key123");
        }

        [Fact]
        public void AuthenticationConfig_CustomAuth_ShouldSetCustomHeaders()
        {
            // Act
            var config = new AuthenticationConfig
            {
                Type = AuthType.Custom,
                CustomHeaders = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "Header1", "Value1" },
                    { "Header2", "Value2" }
                }
            };

            // Assert
            config.Type.Should().Be(AuthType.Custom);
            config.CustomHeaders.Should().HaveCount(2);
            config.CustomHeaders.Should().ContainKey("Header1");
            config.CustomHeaders.Should().ContainKey("Header2");
        }
    }

    public class ValidationRuleTests
    {
        [Fact]
        public void ValidationRule_DefaultValues_ShouldBeSet()
        {
            // Act
            var rule = new ValidationRule();

            // Assert
            rule.Type.Should().Be(ValidationType.StatusCode);
            rule.Target.Should().Be(string.Empty);
            rule.Operator.Should().Be("equals");
            rule.ExpectedValue.Should().BeNull();
            rule.Description.Should().BeNull();
        }

        [Fact]
        public void ValidationRule_WithAllProperties_ShouldSetCorrectly()
        {
            // Act
            var rule = new ValidationRule
            {
                Type = ValidationType.JsonPath,
                Target = "$.name",
                Operator = "equals",
                ExpectedValue = "John",
                Description = "Check user name"
            };

            // Assert
            rule.Type.Should().Be(ValidationType.JsonPath);
            rule.Target.Should().Be("$.name");
            rule.Operator.Should().Be("equals");
            rule.ExpectedValue.Should().Be("John");
            rule.Description.Should().Be("Check user name");
        }
    }

    public class HttpResponseResultTests
    {
        [Fact]
        public void HttpResponseResult_DefaultValues_ShouldBeSet()
        {
            // Act
            var result = new HttpResponseResult();

            // Assert
            result.StatusCode.Should().Be(0);
            result.StatusDescription.Should().Be(string.Empty);
            result.Headers.Should().NotBeNull().And.BeEmpty();
            result.Body.Should().Be(string.Empty);
            result.ResponseTimeMs.Should().Be(0);
            result.ContentLength.Should().Be(0);
            result.ContentType.Should().Be(string.Empty);
            result.IsSuccess.Should().BeFalse();
            result.Timestamp.Should().Be(default);
            result.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public void HttpResponseResult_WithAllProperties_ShouldSetCorrectly()
        {
            // Arrange
            var timestamp = System.DateTime.UtcNow;
            var headers = new System.Collections.Generic.Dictionary<string, string>
            {
                { "Content-Type", "application/json" }
            };

            // Act
            var result = new HttpResponseResult
            {
                StatusCode = 200,
                StatusDescription = "OK",
                Headers = headers,
                Body = "{\"success\":true}",
                ResponseTimeMs = 150,
                ContentLength = 100,
                ContentType = "application/json",
                IsSuccess = true,
                Timestamp = timestamp
            };

            // Assert
            result.StatusCode.Should().Be(200);
            result.StatusDescription.Should().Be("OK");
            result.Headers.Should().HaveCount(1);
            result.Body.Should().Be("{\"success\":true}");
            result.ResponseTimeMs.Should().Be(150);
            result.ContentLength.Should().Be(100);
            result.ContentType.Should().Be("application/json");
            result.IsSuccess.Should().BeTrue();
            result.Timestamp.Should().Be(timestamp);
        }
    }

    public class ValidationResultTests
    {
        [Fact]
        public void ValidationResult_DefaultValues_ShouldBeSet()
        {
            // Act
            var result = new ValidationResult();

            // Assert
            result.IsValid.Should().BeFalse();
            result.Message.Should().Be(string.Empty);
            result.ActualValue.Should().BeNull();
            result.ExpectedValue.Should().BeNull();
            result.ValidationType.Should().Be(ValidationType.StatusCode);
            result.Target.Should().BeNull();
        }

        [Fact]
        public void ValidationResult_WithAllProperties_ShouldSetCorrectly()
        {
            // Act
            var result = new ValidationResult
            {
                IsValid = true,
                Message = "Validation passed",
                ActualValue = 200,
                ExpectedValue = 200,
                ValidationType = ValidationType.StatusCode,
                Target = "StatusCode"
            };

            // Assert
            result.IsValid.Should().BeTrue();
            result.Message.Should().Be("Validation passed");
            result.ActualValue.Should().Be(200);
            result.ExpectedValue.Should().Be(200);
            result.ValidationType.Should().Be(ValidationType.StatusCode);
            result.Target.Should().Be("StatusCode");
        }
    }
}

