using Dev.Tools.Web.Core.WeakReferences;

namespace Dev.Tools.Web.Core.Messaging;

internal sealed class Messenger(WeakReferenceManager weakReferenceManager) : IMessenger
{
    public IDisposable Subscribe<T>(Handler<T> handler) 
        => weakReferenceManager.CreateReference(handler);

    public IDisposable Subscribe<T>(AsyncHandler<T> handler) 
        => weakReferenceManager.CreateReference(handler);

    public async Task Publish<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : IMessage
    {
        await NotifySubscribers(message, cancellationToken).ConfigureAwait(false);
    }

    private async Task NotifySubscribers(IMessage message, CancellationToken cancellationToken)
    {
        foreach (var reference in weakReferenceManager.References)
        {
            var target = reference.Target;
            if (!reference.IsAlive || target is null)
            {
                continue;
            }

            var handlerType = typeof(Handler<>).MakeGenericType(message.GetType());
            if (reference.TargetType == handlerType)
            {
                ((Delegate)target).DynamicInvoke(message);
                continue;
            }

            var asyncHandlerType = typeof(AsyncHandler<>).MakeGenericType(message.GetType());
            if (reference.TargetType != asyncHandlerType)
            {
                continue;
            }
            
            Task? task = (Task?)((Delegate)target).DynamicInvoke(message, cancellationToken);
            if (task is not null && !task.IsCompleted)
            {
                await task.ConfigureAwait(false);
            }
        }
    }
}
