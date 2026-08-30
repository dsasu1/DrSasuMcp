using System.Net;
using System.Text;

namespace DrSasuMcp.Tests.AzureDevOps.TestHelpers
{
    /// <summary>
    /// Test double that answers requests from a list of rules and records what was sent.
    /// </summary>
    public class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly List<Rule> _rules = new();

        /// <summary>
        /// Gets the requests the handler received, in order.
        /// </summary>
        public List<RecordedRequest> Requests { get; } = new();

        /// <summary>
        /// Registers a response for requests whose URL contains every one of the supplied fragments.
        /// </summary>
        public StubHttpMessageHandler RespondTo(string urlFragment, string json, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _rules.Add(new Rule(request => (request.RequestUri?.ToString() ?? string.Empty).Contains(urlFragment, StringComparison.OrdinalIgnoreCase), json, statusCode));
            return this;
        }

        /// <summary>
        /// Registers a response for requests matching a custom predicate.
        /// </summary>
        public StubHttpMessageHandler RespondTo(Func<HttpRequestMessage, bool> predicate, string json, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _rules.Add(new Rule(predicate, json, statusCode));
            return this;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content == null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            Requests.Add(new RecordedRequest(request.Method, request.RequestUri?.ToString() ?? string.Empty, body));

            var rule = _rules.FirstOrDefault(r => r.Predicate(request));
            if (rule == null)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent(
                        $"{{\"message\":\"No stub configured for {request.RequestUri}\"}}",
                        Encoding.UTF8,
                        "application/json")
                };
            }

            return new HttpResponseMessage(rule.StatusCode)
            {
                Content = new StringContent(rule.Json, Encoding.UTF8, "application/json")
            };
        }

        private record Rule(Func<HttpRequestMessage, bool> Predicate, string Json, HttpStatusCode StatusCode);

        public record RecordedRequest(HttpMethod Method, string Url, string? Body);
    }
}
