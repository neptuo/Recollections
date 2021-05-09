using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts
{
    public class PropertyCollection
    {
        private readonly Dictionary<string, UserPropertyModel> storage = new Dictionary<string, UserPropertyModel>();
        private readonly Api api;
        private Task ensureTask;

        public PropertyCollection(Api api)
        {
            Ensure.NotNull(api, "api");
            this.api = api;
        }

        private Task EnsureAsync()
        {
            if (ensureTask == null)
                ensureTask = LoadAsync();

            return ensureTask;
        }

        private async Task LoadAsync()
        {
            var response = await api.GetPropertiesAsync();
            
            storage.Clear();
            foreach (var model in response)
                storage[model.Key] = model;
        }

        public async Task<T> GetAsync<T>(string key, T defaultValue = default)
        {
            Ensure.NotNullOrEmpty(key, "key");

            await EnsureAsync();

            if (storage.TryGetValue(key, out var model))
            {
                if (Converts.Try(model.Value, out T value))
                    return value;

                return defaultValue;
            }

            throw Ensure.Exception.InvalidOperation($"Property '{key}' is not supported.");
        }
    }
}
