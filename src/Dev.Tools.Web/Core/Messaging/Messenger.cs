using System.Reflection;

namespace Dev.Tools.Web.Core.Messaging;

internal sealed class Messenger(WeakReferenceManager weakReferenceManager) : IMessenger
{
    public IDisposable Subscribe<T>(Handler<T> handler)
    {
        if (handler.Target is null)
        {
            throw new ArgumentException(@"Static delegate handlers are not supported", nameof(handler));
        }

        return weakReferenceManager.CreateReference(handler.Target, new Subscription(handler.Method, typeof(T), false));
    }

    public IDisposable Subscribe<T>(AsyncHandler<T> handler)
    {
        if (handler.Target is null)
        {
            throw new ArgumentException(@"Static delegate handlers are not supported", nameof(handler));
        }

        return weakReferenceManager.CreateReference(handler.Target, new Subscription(handler.Method, typeof(T), true));
    }

    public async Task Publish<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : IMessage
    {
        await NotifySubscribers(message, cancellationToken).ConfigureAwait(false);
    }

    private async Task NotifySubscribers(IMessage message, CancellationToken cancellationToken)
    {
        foreach (var reference in weakReferenceManager.References)
        {
            var target = reference.Target;
            if (!reference.IsAlive 
                || target is null 
                || reference.Metadata is not Subscription subscription
                || subscription.MessageType != message.GetType())
            {
                continue;
            }

            if (subscription.IsAsync)
            {
                Task? task = (Task?)subscription.Callback.Invoke(target, [message, cancellationToken]);
                if (task is not null && !task.IsCompleted)
                {
                    await task.ConfigureAwait(false);
                }
            }
            else
            {
                subscription.Callback.Invoke(target, [message]);
            }
        }
    }

    private readonly record struct Subscription(MethodInfo Callback, Type MessageType, bool IsAsync);
}