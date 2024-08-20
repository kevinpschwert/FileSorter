using Microsoft.Identity.Client;

namespace FileSorter.Helpers
{
    public class GraphAuthProvider
    {
        //private static string clientId = "your-client-id";
        //private static string tenantId = "your-tenant-id";
        //private static string clientSecret = "your-client-secret";
        private static string[] scopes = { "https://graph.microsoft.com/.default" };

        public static async Task<string> GetAccessTokenAsync(string clientId, string tenantId, string clientSecret)
        {
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithClientSecret(clientSecret)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
                .Build();

            AuthenticationResult result = await app.AcquireTokenForClient(scopes)
                .ExecuteAsync();

            return result.AccessToken;
        }
    }
}
