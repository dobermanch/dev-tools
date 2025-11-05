using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Dev.Tools.Web.Core.Messaging;

internal sealed class WeakReferenceManager(IOptions<WeakReferencesOptions> options) : BackgroundService
{
    private readonly ConcurrentDictionary<int, Reference> _references = new();

    public ICollection<Reference> References => _references.Values;

    public Reference CreateReference<T>(T obj)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(obj);

        return _references.GetOrAdd(obj.GetHashCode(), _ => new Reference(this, obj));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested)
        {
            try
            {
                var referencesToRemove = _references
                    .Where(it => !it.Value.IsAlive || it.Value.Target is null)
                    .ToArray();

                foreach (var reference in referencesToRemove)
                {
                    _references.TryRemove(reference);
                }

                await Task.Delay(options.Value.CleanupPeriod, stoppingToken);
            }
            catch (OperationCanceledException) { }
        }
    }

    private void Remove(Reference? reference)
    {
        var target = reference?.Target;
        if (target is not null)
        {
            _references.TryRemove(target.GetHashCode(), out _);
        }
    }

    public sealed record Reference : IDisposable
    {
        private readonly WeakReferenceManager _manager;
        private readonly WeakReference _reference;

        public Reference(WeakReferenceManager manager, object obj)
        {
            _manager = manager;
            _reference = new WeakReference(obj);
            TargetType = obj.GetType();
        }

        public object? Target => _reference.Target;

        public bool IsAlive => _reference.IsAlive;

        public Type TargetType { get; }

        public void Dispose() => _manager.Remove(this);
    }
}