using MediatR;

namespace FlowCart.BuildingBlocks.Application;

/// <summary>
/// CQRS Command interface returning Result of TResponse.
/// </summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}

/// <summary>
/// CQRS Command interface returning Result.
/// </summary>
public interface ICommand : IRequest<Result>
{
}

/// <summary>
/// CQRS Command Handler interface returning Result of TResponse.
/// </summary>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>
{
}

/// <summary>
/// CQRS Command Handler interface returning Result.
/// </summary>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand
{
}

/// <summary>
/// CQRS Query interface returning Result of TResponse.
/// </summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}

/// <summary>
/// CQRS Query Handler interface returning Result of TResponse.
/// </summary>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}
