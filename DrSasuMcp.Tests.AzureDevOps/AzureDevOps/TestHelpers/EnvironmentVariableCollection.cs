using Xunit;

namespace DrSasuMcp.Tests.AzureDevOps.TestHelpers
{
    /// <summary>
    /// Groups test classes that set the process-wide Azure DevOps environment variables, so they never run in parallel.
    /// </summary>
    [CollectionDefinition(Name)]
    public class EnvironmentVariableCollection
    {
        public const string Name = "Azure DevOps environment variables";
    }
}
