namespace Cloudsoft.Core.Options;

public class FeatureFlagsOptions
{
    public const string SectionName = "FeatureFlags";

    public bool UseMongoDb { get; set; }

    public bool UseAzureKeyVault { get; set; }
}
