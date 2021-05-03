using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections
{
    public static class HttpClientJsonExtensions
    {
        public async static Task<TResponse> PostAsJsonAsync<TRequest, TResponse>(this HttpClient http, string url, TRequest request)
        {
            var response = await http.PostAsJsonAsync(url, request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TResponse>();
        }

        public async static Task<TResponse> PutAsJsonAsync<TRequest, TResponse>(this HttpClient http, string url, TRequest request)
        {
            var response = await http.PutAsJsonAsync(url, request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TResponse>();
        }
    }
}
