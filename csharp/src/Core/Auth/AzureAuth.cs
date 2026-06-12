using Azure.Identity;

namespace Scripts.Core.Auth;

internal static class AzureAuth
{
	public static readonly DefaultAzureCredential Credential = new();
}
