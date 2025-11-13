using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Dev.Tools.Web.Core.Messaging;

internal sealed class WeakReferenceManager(IOptions<WeakReferencesOptions> options) : BackgroundService
{
    private readonly ConcurrentDictionary<SubscriptionKey, IReference> _references = new();

    public ICollection<IReference> References => _references.Values;

    public IReference CreateReference<T>(T referenceForObj, object? metadata = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(referenceForObj);

        var key = new SubscriptionKey(RuntimeHelpers.GetHashCode(referenceForObj), metadata?.GetHashCode() ?? 0);
        return _references.GetOrAdd(key, _ => new Reference(key, this, referenceForObj, metadata));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var referencesToRemove = _references
                    .Where(it => !it.Value.IsAlive || it.Value.Target is null)
                    .ToArray();

                foreach (var reference in referencesToRemove)
                {
                    _references.TryRemove(reference.Key, out _);
                }

                await Task.Delay(options.Value.CleanupPeriod, stoppingToken);
            }
            catch (OperationCanceledException) { }
        }
    }

    private void Remove(SubscriptionKey key) 
        => _references.TryRemove(key, out _);

    public interface IReference : IDisposable
    {
        public object? Target { get; }

        public bool IsAlive { get; }
        
        public object? Metadata { get; }
    }

    private readonly record struct SubscriptionKey(int ObjectHashCode, int MetadataHashCode);

    private sealed record Reference : IReference
    {
        private readonly WeakReferenceManager _manager;
        private readonly WeakReference _reference;
        private readonly SubscriptionKey _key;
        
        // NOTE: do not convert to primary constructor, because it will generate hard reference to the obj
        internal Reference(SubscriptionKey key, WeakReferenceManager manager, object obj, object? metadata)
        {
            _manager = manager;
            _reference = new WeakReference(obj);
            _key = key;
            Metadata = metadata;
        }

        public object? Target => _reference.Target;

        public bool IsAlive => _reference.IsAlive;
        
        public object? Metadata { get; }

        public void Dispose() => _manager.Remove(_key);
    }
}