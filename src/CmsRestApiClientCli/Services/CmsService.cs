using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CmsRestApiClientCli.Services;

public class CmsService(HttpClient httpClient)
{
    /// <summary>
    /// List items by resource type; contenttypes, propertygroups or displaytemplates.
    /// </summary>
    /// <param name="resourceType">Set to contenttypes, propertygroups or displaytemplates.</param>
    /// <param name="accessToken">An access token.</param>
    /// <returns>A JSON response as a string.</returns>
    public async Task<string> List(string resourceType, string accessToken)
    {
        var message = new HttpRequestMessage(HttpMethod.Get, $"_cms/preview2/{resourceType}?pageSize=10000");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await httpClient.SendAsync(message);
        var responseAsString = await response.Content.ReadAsStringAsync();

        return responseAsString;
    }

    public async Task<string> Upsert(string resourceType, string key, string content, bool ignoreDataLossWarnings, string accessToken)
    {
        var requestUri = $"_cms/preview2/{resourceType}/{key}";

        if (ignoreDataLossWarnings)
        {
            requestUri += "?ignoreDataLossWarnings=true";
        }

        var message = new HttpRequestMessage(HttpMethod.Put, requestUri);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        message.Content = new StringContent(content, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
        message.Content.Headers.ContentLength = content.Length;

        var response = await httpClient.SendAsync(message);
        var responseAsString = await response.Content.ReadAsStringAsync();
        return responseAsString;
    }

    public async Task<string> Delete(string resourceType, string key, string accessToken)
    {
        var message = new HttpRequestMessage(HttpMethod.Delete, $"_cms/preview2/{resourceType}/{key}");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await httpClient.SendAsync(message);
        var responseAsString = await response.Content.ReadAsStringAsync();

        return responseAsString;
    }
}