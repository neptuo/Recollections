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
        public static Task<TModel> PostJsonAsync<TModel>(this HttpClient http, string url, TModel request)
            => PostJsonAsync<TModel, TModel>(http, url, request);

        public async static Task<TResponse> PostJsonAsync<TRequest, TResponse>(this HttpClient http, string url, TRequest request)
        {
            var response = await http.PostAsJsonAsync(url, request);
            return await response.Content.ReadFromJsonAsync<TResponse>();
        }

        public static Task<TModel> PutJsonAsync<TModel>(this HttpClient http, string url, TModel request)
            => PutJsonAsync<TModel, TModel>(http, url, request);

        public async static Task<TResponse> PutJsonAsync<TRequest, TResponse>(this HttpClient http, string url, TRequest request)
        {
            var response = await http.PutAsJsonAsync(url, request);
            return await response.Content.ReadFromJsonAsync<TResponse>();
        }
    }
}
