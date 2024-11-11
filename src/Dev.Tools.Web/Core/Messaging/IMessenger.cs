using MediatR;

namespace Dev.Tools.Web.Core.Messaging;

public delegate void Handler<in T>(T message);

public delegate Task AsyncHandler<in T>(T message, CancellationToken cancellationToken);

public interface IMessenger : IMediator
{
    IDisposable Subscribe<T>(Handler<T> handler);

    IDisposable Subscribe<T>(AsyncHandler<T> handler);
}
