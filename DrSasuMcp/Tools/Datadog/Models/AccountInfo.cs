namespace DrSasuMcp.Tools.Datadog.Models
{
    /// <summary>
    /// Represents Datadog account/organization information.
    /// </summary>
    public class AccountInfo
    {
        public string? Name { get; set; }
        public string? PublicId { get; set; }
        public string? Subscription { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? Region { get; set; }
    }
}

