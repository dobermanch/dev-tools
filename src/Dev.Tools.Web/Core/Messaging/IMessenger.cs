namespace Dev.Tools.Web.Core.Messaging;

public delegate void Handler<in T>(T message);

public delegate Task AsyncHandler<in T>(T message, CancellationToken cancellationToken);

public interface IMessenger
{
    IDisposable Subscribe<T>(Handler<T> handler);

    IDisposable Subscribe<T>(AsyncHandler<T> handler);
    
    Task Publish<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : IMessage;
}