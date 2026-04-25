namespace Cloudsoft.Core.Options;

public class KeyVaultOptions
{
    public const string SectionName = "KeyVault";

    public string VaultUri { get; set; } = string.Empty;

    public string ManagedIdentityClientId { get; set; } = string.Empty;
}
