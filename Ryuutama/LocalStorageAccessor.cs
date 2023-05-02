using Microsoft.JSInterop;
using System.Text.Json;

namespace Ryuutama
{
    public class LocalStorageAccessor : IAsyncDisposable
    {
        private Lazy<IJSObjectReference> _accessorJsRef = new();
        private readonly IJSRuntime _jsRuntime;

        public LocalStorageAccessor(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        private async Task WaitForReference()
        {
            if (_accessorJsRef.IsValueCreated is false)
            {
                _accessorJsRef = new(await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "/js/LocalStorageAccessor.js"));
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_accessorJsRef.IsValueCreated)
            {
                await _accessorJsRef.Value.DisposeAsync();
            }
        }

        public async Task<T?> GetValueAsync<T>(string key)
        {
            await WaitForReference();
            var serialized = await _accessorJsRef.Value.InvokeAsync<string>("get", key);
            if(serialized == null)
            {
                return default;
            }    
            var result = JsonSerializer.Deserialize<T>(serialized);

            return result;
        }

        public async Task SetValueAsync<T>(string key, T value)
        {
            await WaitForReference();
            var converted = JsonSerializer.Serialize(value);
            await _accessorJsRef.Value.InvokeVoidAsync("set", key, converted);
        }

        public async Task Clear()
        {
            await WaitForReference();
            await _accessorJsRef.Value.InvokeVoidAsync("clear");
        }

        public async Task RemoveAsync(string key)
        {
            await WaitForReference();
            await _accessorJsRef.Value.InvokeVoidAsync("remove", key);
        }
    }
}
