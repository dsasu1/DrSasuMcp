using System.Text;

namespace DrSasuMcp.Tools.Datadog.Utils
{
    /// <summary>
    /// Utility class for building Datadog queries.
    /// </summary>
    public static class QueryBuilder
    {
        /// <summary>
        /// Builds a metric query string.
        /// </summary>
        public static string BuildMetricQuery(string metric, string? aggregation = null, Dictionary<string, string>? tags = null)
        {
            var query = new StringBuilder();
            
            // Add aggregation
            if (!string.IsNullOrWhiteSpace(aggregation))
            {
                query.Append($"{aggregation}:");
            }
            
            // Add metric name
            query.Append(metric);
            
            // Add tags
            if (tags != null && tags.Count > 0)
            {
                query.Append('{');
                var tagParts = tags.Select(kvp => $"{kvp.Key}:{kvp.Value}");
                query.Append(string.Join(",", tagParts));
                query.Append('}');
            }
            
            return query.ToString();
        }

        /// <summary>
        /// Builds a log query string.
        /// </summary>
        public static string BuildLogQuery(string? text = null, Dictionary<string, string>? filters = null)
        {
            var queryParts = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(text))
            {
                queryParts.Add(text);
            }
            
            if (filters != null && filters.Count > 0)
            {
                foreach (var filter in filters)
                {
                    queryParts.Add($"{filter.Key}:{filter.Value}");
                }
            }
            
            return string.Join(" ", queryParts);
        }

        /// <summary>
        /// Builds a trace query string.
        /// </summary>
        public static string BuildTraceQuery(string? service = null, string? operation = null, Dictionary<string, string>? tags = null)
        {
            var queryParts = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(service))
            {
                queryParts.Add($"service:{service}");
            }
            
            if (!string.IsNullOrWhiteSpace(operation))
            {
                queryParts.Add($"operation:{operation}");
            }
            
            if (tags != null && tags.Count > 0)
            {
                foreach (var tag in tags)
                {
                    queryParts.Add($"{tag.Key}:{tag.Value}");
                }
            }
            
            return string.Join(" ", queryParts);
        }
    }
}

