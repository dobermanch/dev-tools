using System.Text.Json;
using Dev.Tools.Web.Core.Serializer;
using Microsoft.JSInterop;

namespace Dev.Tools.Web.Core.Storage;

internal class BrowserStorageProvider(IJSRuntime jSRuntime, IJsonSerializer serializer) : IStorageProvider
{
    public async ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));
        
        var serialisedData = serializer.Serialize(data);
        await SetItemAsync(key, serialisedData, cancellationToken).ConfigureAwait(false);
    }
    
    public async ValueTask<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        var serialisedData = await GetItemAsync(key, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(serialisedData))
            return default;

        try
        {
            return serializer.Deserialize<T>(serialisedData);
        }
        catch (JsonException e) when (e.Path == "$" && typeof(T) == typeof(string))
        {
            // For backward compatibility return the plain string.
            // On the next save a correct value will be stored and this Exception will not happen again, for this 'key'
            return (T)(object)serialisedData;
        }
    }

    public async ValueTask<string?> GetItemAsync(string key, CancellationToken cancellationToken = default)
        => await InvokeWrapperAsync(async () =>
            await jSRuntime.InvokeAsync<string?>("localStorage.getItem", cancellationToken, key)
        );

    public async ValueTask SetItemAsync(string key, string data, CancellationToken cancellationToken = default)
        => await InvokeWrapperAsync<object>(async () =>{
            await jSRuntime.InvokeVoidAsync("localStorage.setItem", cancellationToken, key, data);
            return default!;
        });

    public async ValueTask ClearAsync(CancellationToken cancellationToken = default)
        => await InvokeWrapperAsync<object>(async () =>{
            await jSRuntime.InvokeVoidAsync("localStorage.clear", cancellationToken);
            return default!;
        });

    public async ValueTask<bool> ContainKeyAsync(string key, CancellationToken cancellationToken = default)
        => await InvokeWrapperAsync(async () =>
            await jSRuntime.InvokeAsync<bool>("localStorage.hasOwnProperty", cancellationToken, key)
        );

    public async ValueTask<string?> KeyAsync(int index, CancellationToken cancellationToken = default)
        => await InvokeWrapperAsync(async () =>
            await jSRuntime.InvokeAsync<string?>("localStorage.key", cancellationToken, index)
        );

    public async ValueTask<IEnumerable<string>> KeysAsync(CancellationToken cancellationToken = default)
        => await InvokeWrapperAsync(async () =>
            await jSRuntime.InvokeAsync<IEnumerable<string>>("eval", cancellationToken, "Object.keys(localStorage)")
        );

    public async ValueTask<int> LengthAsync(CancellationToken cancellationToken = default)
        => await InvokeWrapperAsync(async () =>
            await jSRuntime.InvokeAsync<int>("eval", cancellationToken, "localStorage.length")
        );

    public async ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default)
        => _ = await InvokeWrapperAsync<object>(async () =>
        {
            await jSRuntime.InvokeVoidAsync("localStorage.removeItem", cancellationToken, key);
            return default!;
        });

    private async ValueTask<T> InvokeWrapperAsync<T>(Func<ValueTask<T>> invokeAsync)
    {
        try
        {
            return await invokeAsync();
        }
        catch (Exception exception)
        {
            if (IsStorageDisabledException(exception))
            {
                throw new Exception("Unable to access the browser storage. This is most likely due to the browser settings.", exception);
            }

            throw;
        }
    }

    private static bool IsStorageDisabledException(Exception exception)
        => exception.Message.Contains("Failed to read the 'localStorage' property from 'Window'");
}