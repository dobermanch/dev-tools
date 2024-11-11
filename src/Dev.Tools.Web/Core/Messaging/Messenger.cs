using Dev.Tools.Web.Core.WeakReferences;
using MediatR;

namespace Dev.Tools.Web.Core.Messaging;

internal sealed class Messenger(IServiceProvider provider, WeakReferenceManager weakReferenceManager) 
    : Mediator(provider), IMessenger
{
    public IDisposable Subscribe<T>(Handler<T> handler) 
        => weakReferenceManager.CreateReference(handler);

    public IDisposable Subscribe<T>(AsyncHandler<T> handler) 
        => weakReferenceManager.CreateReference(handler);

    protected override async Task PublishCore(IEnumerable<NotificationHandlerExecutor> handlerExecutors, INotification notification, CancellationToken cancellationToken)
    {
        await NotifySubscribers(notification, cancellationToken).ConfigureAwait(false);
        await base.PublishCore(handlerExecutors, notification, cancellationToken);
    }

    private async Task NotifySubscribers(INotification notification, CancellationToken cancellationToken)
    {
        foreach (var reference in weakReferenceManager.References)
        {
            var target = reference.Target;
            if (!reference.IsAlive || target is null)
            {
                continue;
            }

            var handlerType = typeof(Handler<>).MakeGenericType(notification.GetType());
            if (reference.TargetType == handlerType)
            {
                ((Delegate)target).DynamicInvoke(notification);
                continue;
            }

            var asyncHandlerType = typeof(AsyncHandler<>).MakeGenericType(notification.GetType());
            if (reference.TargetType != asyncHandlerType)
            {
                continue;
            }
            
            Task? task = (Task?)((Delegate)target).DynamicInvoke(notification, cancellationToken);
            if (task is not null && !task.IsCompleted)
            {
                await task.ConfigureAwait(false);
            }
        }
    }
}
