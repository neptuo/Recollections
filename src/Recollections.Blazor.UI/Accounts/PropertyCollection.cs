using Neptuo;
using Neptuo.Logging;
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
        private readonly Dictionary<string, UserPropertyModel> storage = new();
        private readonly Api api;
        private readonly ILog<PropertyCollection> log;
        private Task ensureTask;
        private bool wasLoadRequested = false;

        public event Action ValuesLoaded;

        public PropertyCollection(Api api, ILog<PropertyCollection> log)
        {
            Ensure.NotNull(api, "api");
            Ensure.NotNull(log, "log");
            this.api = api;
            this.log = log;
        }

        private Task EnsureAsync()
        {
            if (ensureTask == null)
            {
                ensureTask = LoadAsync();
                ensureTask.ContinueWith(t => ValuesLoaded?.Invoke());
            }

            return ensureTask;
        }

        private async Task LoadAsync()
        {
            log.Debug("Loading properties");
            var response = await api.GetPropertiesAsync();

            storage.Clear();
            log.Debug($"Got '{response.Count}' items");
            foreach (var model in response)
                storage[model.Key] = model;
        }

        public async Task<T> GetAsync<T>(string key, T defaultValue = default)
        {
            Ensure.NotNullOrEmpty(key, "key");

            wasLoadRequested = true;

            if (!api.IsAuthorized)
                return defaultValue;

            await EnsureAsync();

            if (storage.TryGetValue(key, out var model))
            {
                log.Debug($"Found property '{key}' with value '{model.Value}'");
                if (model.Value != null && Converts.Try(model.Value, out T value))
                    return value;

                log.Debug($"Failed to convert '{key}' with value '{model.Value}' to type '{typeof(T).Name}'");
                return defaultValue;
            }

            throw NotSupportedKey(key);
        }

        public async Task SetAsync<T>(string key, T value)
        {
            Ensure.NotNull(key, "key");

            if (!api.IsAuthorized)
                return;

            if (!storage.TryGetValue(key, out var model))
                throw NotSupportedKey(key);

            string rawValue = null;
            if (value != null && !Converts.Try(value, out rawValue))
                throw Ensure.Exception.InvalidOperation($"Property type '{typeof(T).Name}' is not supported.");

            model.Value = rawValue;

            await api.SetPropertyAsync(model);
            
            ValuesLoaded?.Invoke();
        }

        private InvalidOperationException NotSupportedKey(string key)
        {
            log.Debug($"Going to throw for unsupported key '{key}'");
            return Ensure.Exception.InvalidOperation($"Property '{key}' is not supported.");
        }

        internal void ClearOnUserChanged()
        {
            log.Debug($"Clear on user changed, {(wasLoadRequested ? "previously has properties" : "not loaded yet")}");

            storage.Clear();
            ensureTask = null;

            if (wasLoadRequested)
            {
                if (api.IsAuthorized)
                    _ = EnsureAsync();
                else
                    ValuesLoaded?.Invoke();
            }
        }
    }
}
