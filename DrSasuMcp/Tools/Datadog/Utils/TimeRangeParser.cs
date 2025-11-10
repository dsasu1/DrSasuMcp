namespace DrSasuMcp.Tools.Datadog.Utils
{
    /// <summary>
    /// Utility class for parsing time ranges and converting to DateTime.
    /// </summary>
    public static class TimeRangeParser
    {
        /// <summary>
        /// Parses a time string and returns a DateTime.
        /// Supports:
        /// - ISO 8601 format (e.g., "2024-01-01T00:00:00Z")
        /// - Relative time (e.g., "1h ago", "30m ago", "2d ago")
        /// - "now" for current time
        /// </summary>
        public static DateTime ParseTime(string timeString)
        {
            if (string.IsNullOrWhiteSpace(timeString))
            {
                return DateTime.UtcNow;
            }

            // Handle "now"
            if (timeString.Equals("now", StringComparison.OrdinalIgnoreCase))
            {
                return DateTime.UtcNow;
            }

            // Handle relative time (e.g., "1h ago", "30m ago", "2d ago")
            if (timeString.EndsWith(" ago", StringComparison.OrdinalIgnoreCase))
            {
                var timePart = timeString.Substring(0, timeString.Length - 4).Trim();
                return ParseRelativeTime(timePart);
            }

            // Try parsing as ISO 8601
            if (DateTime.TryParse(timeString, out var parsedDateTime))
            {
                // If no timezone specified, assume UTC
                if (parsedDateTime.Kind == DateTimeKind.Unspecified)
                {
                    return DateTime.SpecifyKind(parsedDateTime, DateTimeKind.Utc);
                }
                return parsedDateTime.ToUniversalTime();
            }

            // Try parsing as relative time without "ago" (e.g., "1h", "30m")
            try
            {
                return ParseRelativeTime(timeString);
            }
            catch
            {
                throw new ArgumentException($"Unable to parse time string: {timeString}");
            }
        }

        private static DateTime ParseRelativeTime(string timeString)
        {
            timeString = timeString.Trim();

            // Extract number and unit
            var numberStr = "";
            var unit = "";
            
            for (int i = 0; i < timeString.Length; i++)
            {
                if (char.IsDigit(timeString[i]) || timeString[i] == '.')
                {
                    numberStr += timeString[i];
                }
                else
                {
                    unit = timeString.Substring(i).Trim().ToLowerInvariant();
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(numberStr) || !double.TryParse(numberStr, out var number))
            {
                throw new ArgumentException($"Invalid time format: {timeString}");
            }

            var now = DateTime.UtcNow;
            var timeSpan = unit switch
            {
                "s" or "sec" or "second" or "seconds" => TimeSpan.FromSeconds(number),
                "m" or "min" or "minute" or "minutes" => TimeSpan.FromMinutes(number),
                "h" or "hr" or "hour" or "hours" => TimeSpan.FromHours(number),
                "d" or "day" or "days" => TimeSpan.FromDays(number),
                "w" or "week" or "weeks" => TimeSpan.FromDays(number * 7),
                "mo" or "month" or "months" => TimeSpan.FromDays(number * 30), // Approximate
                _ => throw new ArgumentException($"Unknown time unit: {unit}")
            };

            return now.Subtract(timeSpan);
        }

        /// <summary>
        /// Parses a time range string and returns start and end times.
        /// Supports formats like:
        /// - "1h" (last hour)
        /// - "24h" (last 24 hours)
        /// - "1h ago" to "now"
        /// </summary>
        public static (DateTime from, DateTime to) ParseTimeRange(string timeRange, DateTime? to = null)
        {
            var endTime = to ?? DateTime.UtcNow;
            
            if (string.IsNullOrWhiteSpace(timeRange))
            {
                return (endTime.AddHours(-1), endTime);
            }

            // Try parsing as relative duration (e.g., "1h", "30m")
            var timeString = timeRange.Trim();
            var numberStr = "";
            var unit = "";
            
            for (int i = 0; i < timeString.Length; i++)
            {
                if (char.IsDigit(timeString[i]) || timeString[i] == '.')
                {
                    numberStr += timeString[i];
                }
                else
                {
                    unit = timeString.Substring(i).Trim().ToLowerInvariant();
                    break;
                }
            }

            if (!string.IsNullOrWhiteSpace(numberStr) && double.TryParse(numberStr, out var number))
            {
                var timeSpan = unit switch
                {
                    "s" or "sec" or "second" or "seconds" => TimeSpan.FromSeconds(number),
                    "m" or "min" or "minute" or "minutes" => TimeSpan.FromMinutes(number),
                    "h" or "hr" or "hour" or "hours" => TimeSpan.FromHours(number),
                    "d" or "day" or "days" => TimeSpan.FromDays(number),
                    "w" or "week" or "weeks" => TimeSpan.FromDays(number * 7),
                    "mo" or "month" or "months" => TimeSpan.FromDays(number * 30), // Approximate
                    _ => TimeSpan.Zero
                };

                if (timeSpan != TimeSpan.Zero)
                {
                    var startTime = endTime.Subtract(timeSpan);
                    return (startTime, endTime);
                }
            }

            // If it's not a duration, try parsing as a single time point
            var timePoint = ParseTime(timeRange);
            return (timePoint, endTime);
        }
    }
}

