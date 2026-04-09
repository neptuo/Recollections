using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace Neptuo.Recollections.Tests.Infrastructure;

public static class ApiClientExtensions
{
    public static void SetUser(this HttpClient client, string userId, string userName)
    {
        client.DefaultRequestHeaders.Remove(TestAuthHandler.UserIdHeader);
        client.DefaultRequestHeaders.Remove(TestAuthHandler.UserNameHeader);
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId);
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserNameHeader, userName);
    }

    public static void SetAnonymous(this HttpClient client)
    {
        client.DefaultRequestHeaders.Remove(TestAuthHandler.UserIdHeader);
        client.DefaultRequestHeaders.Remove(TestAuthHandler.UserNameHeader);
        client.DefaultRequestHeaders.Authorization = null;
    }

    public static async Task<T> ReadJsonAsync<T>(this HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<T>(json);
        if (result is null)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize response body to '{typeof(T).FullName}'. Response body: {json}"
            );
        }

        return result;
    }
}
