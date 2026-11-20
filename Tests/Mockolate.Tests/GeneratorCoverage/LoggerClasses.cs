using System;
using Microsoft.Extensions.Logging;

namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Classes that implement <see cref="ILogger" /> with overrides: the mock overrides the most derived override, but
///     lists the slot under its virtual or abstract declaration.
/// </summary>
public abstract class AbstractLoggerBase : ILogger
{
	public abstract IDisposable? BeginScope<TState>(TState state) where TState : notnull;

	public abstract bool IsEnabled(LogLevel logLevel);

	public abstract void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
		Func<TState, Exception?, string> formatter);
}

public class ConcreteLogger : AbstractLoggerBase
{
	public override IDisposable? BeginScope<TState>(TState state)
		=> null;

	public override bool IsEnabled(LogLevel logLevel)
		=> false;

	public override void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
		Func<TState, Exception?, string> formatter)
	{
	}
}

public class VirtualLoggerBase : ILogger<VirtualLoggerBase>
{
	public virtual IDisposable? BeginScope<TState>(TState state) where TState : notnull
		=> null;

	public virtual bool IsEnabled(LogLevel logLevel)
		=> false;

	public virtual void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
		Func<TState, Exception?, string> formatter)
	{
	}
}

public class MiddleLogger : VirtualLoggerBase
{
	public override void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
		Func<TState, Exception?, string> formatter)
	{
	}
}

public class LeafLogger : MiddleLogger
{
}
