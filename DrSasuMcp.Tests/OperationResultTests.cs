using DrSasuMcp.Tools;
using FluentAssertions;
using Xunit;

namespace DrSasuMcp.Tests
{
    public class OperationResultTests
    {
        [Fact]
        public void Constructor_WithSuccessOnly_ShouldSetProperties()
        {
            // Act
            var result = new OperationResult(success: true);

            // Assert
            result.Success.Should().BeTrue();
            result.Error.Should().BeNull();
            result.RowsAffected.Should().BeNull();
            result.Data.Should().BeNull();
        }

        [Fact]
        public void Constructor_WithAllParameters_ShouldSetAllProperties()
        {
            // Arrange
            var testData = new { Id = 1, Name = "Test" };

            // Act
            var result = new OperationResult(
                success: true,
                error: null,
                rowsAffected: 5,
                data: testData
            );

            // Assert
            result.Success.Should().BeTrue();
            result.Error.Should().BeNull();
            result.RowsAffected.Should().Be(5);
            result.Data.Should().Be(testData);
        }

        [Fact]
        public void Constructor_WithError_ShouldSetErrorMessage()
        {
            // Act
            var result = new OperationResult(
                success: false,
                error: "An error occurred"
            );

            // Assert
            result.Success.Should().BeFalse();
            result.Error.Should().Be("An error occurred");
            result.RowsAffected.Should().BeNull();
            result.Data.Should().BeNull();
        }

        [Fact]
        public void Constructor_WithRowsAffected_ShouldSetRowsAffected()
        {
            // Act
            var result = new OperationResult(
                success: true,
                error: null,
                rowsAffected: 10
            );

            // Assert
            result.Success.Should().BeTrue();
            result.RowsAffected.Should().Be(10);
        }

        [Fact]
        public void Constructor_WithData_ShouldSetData()
        {
            // Arrange
            var testData = new[] { 1, 2, 3, 4, 5 };

            // Act
            var result = new OperationResult(
                success: true,
                data: testData
            );

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(testData);
        }

        [Fact]
        public void Constructor_WithZeroRowsAffected_ShouldWork()
        {
            // Act
            var result = new OperationResult(
                success: true,
                rowsAffected: 0
            );

            // Assert
            result.Success.Should().BeTrue();
            result.RowsAffected.Should().Be(0);
        }

        [Fact]
        public void Constructor_WithNegativeRowsAffected_ShouldWork()
        {
            // Act
            var result = new OperationResult(
                success: true,
                rowsAffected: -1
            );

            // Assert
            result.Success.Should().BeTrue();
            result.RowsAffected.Should().Be(-1);
        }
    }
}

