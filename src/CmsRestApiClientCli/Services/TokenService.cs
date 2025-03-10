using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CmsRestApiClientCli.Services;

public class TokenService(HttpClient httpClient)
{
    public async Task<string> GetAccessToken(string clientId, string clientSecret)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, "_cms/preview2/oauth/token");
        message.Content = new StringContent($$"""
                                            {
                                              "grant_type": "client_credentials",
                                              "client_id": "{{clientId}}",
                                              "client_secret": "{{clientSecret}}"
                                            }
                                            """);
        message.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var response = await httpClient.SendAsync(message);
        var responseAsString = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseAsString);

        return tokenResponse.AccessToken;
    }

    public class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }
    }
}
