namespace Dev.Tools.Web.Core.Storage;

public interface IStorageProvider
{
    ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken = default);

    ValueTask<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default);

    ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default);
}