using DrSasuMcp.Tools.Datadog.Utils;
using FluentAssertions;
using Xunit;

namespace DrSasuMcp.Tests.Datadog.Utils
{
    public class TimeRangeParserTests
    {
        [Fact]
        public void ParseTime_WithNow_ReturnsCurrentTime()
        {
            // Act
            var result = TimeRangeParser.ParseTime("now");

            // Assert
            result.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void ParseTime_WithEmptyString_ReturnsCurrentTime()
        {
            // Act
            var result = TimeRangeParser.ParseTime("");

            // Assert
            result.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Theory]
        [InlineData("1h ago")]
        [InlineData("30m ago")]
        [InlineData("2d ago")]
        [InlineData("1w ago")]
        public void ParseTime_WithRelativeTimeAgo_ReturnsPastTime(string timeString)
        {
            // Act
            var result = TimeRangeParser.ParseTime(timeString);

            // Assert
            result.Should().BeBefore(DateTime.UtcNow);
        }

        [Fact]
        public void ParseTime_WithISO8601_ReturnsParsedDateTime()
        {
            // Arrange
            var isoTime = "2024-01-01T00:00:00Z";

            // Act
            var result = TimeRangeParser.ParseTime(isoTime);

            // Assert
            result.Year.Should().Be(2024);
            result.Month.Should().Be(1);
            result.Day.Should().Be(1);
        }

        [Theory]
        [InlineData("1h", 1)]
        [InlineData("30m", 0.5)]
        [InlineData("2d", 2)]
        [InlineData("1w", 7)]
        public void ParseTimeRange_WithDuration_ReturnsCorrectRange(string duration, double expectedHours)
        {
            // Act
            var (from, to) = TimeRangeParser.ParseTimeRange(duration);

            // Assert
            to.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            var timeSpan = to - from;
            timeSpan.TotalHours.Should().BeApproximately(expectedHours, 0.1);
        }

        [Fact]
        public void ParseTimeRange_WithEmptyString_ReturnsDefaultRange()
        {
            // Act
            var (from, to) = TimeRangeParser.ParseTimeRange("");

            // Assert
            to.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            var timeSpan = to - from;
            timeSpan.TotalHours.Should().Be(1);
        }

        [Fact]
        public void ParseTime_WithInvalidFormat_ThrowsArgumentException()
        {
            // Act & Assert
            var act = () => TimeRangeParser.ParseTime("invalid-time");
            act.Should().Throw<ArgumentException>();
        }
    }
}

