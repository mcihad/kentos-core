namespace Kentos.SharedKernel.Cqrs;

/// <summary>A state-changing operation returning a response (handled by Wolverine).</summary>
/// <typeparam name="TResponse">The operation result.</typeparam>
public interface ICommand<TResponse>
{
}

/// <summary>A command that returns no response.</summary>
public interface ICommand
{
}
