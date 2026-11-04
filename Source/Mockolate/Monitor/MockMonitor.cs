using System;
using System.Linq;
using Mockolate.Interactions;
using Mockolate.Internals;

namespace Mockolate.Monitor;

/// <summary>
///     Provides monitoring capabilities for a mocked instance of the specified type, allowing inspection of accessed
///     properties, invoked methods, and event subscriptions.
/// </summary>
/// <remarks>
///     Use this class to track and analyze interactions with a mock, such as which members were accessed or
///     which events were subscribed to, during a test session. Monitoring is session-based; begin a session with the Run
///     method and dispose the returned scope to finalize monitoring.
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public abstract class MockMonitor
{
	private readonly IMockInteractions _monitoredInteractions;
	private int _monitoringStart = -1;

	/// <inheritdoc cref="MockMonitor" />
	protected MockMonitor(IMockInteractions mockInteractions)
	{
		_monitoredInteractions = mockInteractions;
		Interactions = new FastMockInteractions(0, mockInteractions.SkipInteractionRecording);
	}

	/// <summary>
	///     The interactions that were recorded during the monitoring session.
	/// </summary>
	protected IMockInteractions Interactions { get; }

	/// <summary>
	///     Begins monitoring interactions and returns a scope that ends monitoring when disposed.
	/// </summary>
	/// <remarks>
	///     Dispose the returned object to stop monitoring and finalize the session.
	/// </remarks>
	/// <returns>
	///     An <see cref="IDisposable" /> that ends the monitoring session when disposed.
	/// </returns>
	public IDisposable Run()
	{
		if (_monitoringStart >= 0)
		{
			throw new InvalidOperationException(
				"Monitoring is already running. Dispose the previous scope before starting a new one.");
		}

		_monitoringStart = _monitoredInteractions.Count;
		_monitoredInteractions.OnClearing += OnClearing;
		return new MonitorScope(Stop);
	}

	private void Stop()
	{
		if (_monitoringStart >= 0)
		{
			foreach (IInteraction interaction in _monitoredInteractions.Skip(_monitoringStart))
			{
				Interactions.RegisterInteraction(interaction);
			}
		}

		_monitoredInteractions.OnClearing -= OnClearing;
		_monitoringStart = -1;
	}

	private void OnClearing(object? sender, EventArgs e)
	{
		_monitoringStart = 0;
		Interactions.Clear();
	}

	/// <summary>
	///     Updates the interactions while the mock monitor is running.
	/// </summary>
	protected void UpdateInteractions()
	{
		if (_monitoringStart >= 0)
		{
			int copied = 0;
			foreach (IInteraction interaction in _monitoredInteractions.Skip(_monitoringStart))
			{
				Interactions.RegisterInteraction(interaction);
				copied++;
			}

			_monitoringStart += copied;
		}
	}

	private sealed class MonitorScope(Action callback) : IDisposable
	{
		public void Dispose() => callback();
	}
}

/// <summary>
///     Provides monitoring capabilities for a mocked instance of the specified type, allowing inspection of accessed
///     properties, invoked methods, and event subscriptions.
/// </summary>
/// <remarks>
///     Use this class to track and analyze interactions with a mock, such as which members were accessed or
///     which events were subscribed to, during a test session.<br />
///     Monitoring is session-based: begin a session with the <see cref="MockMonitor.Run()" /> method and
///     dispose the returned scope to finalize monitoring.
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public sealed class MockMonitor<T> : MockMonitor
{
	/// <inheritdoc cref="MockMonitor{T}" />
	public MockMonitor(IMockInteractions interactions, Func<IMockInteractions, T> verify) : base(interactions)
	{
		Verify = verify(Interactions);
	}

	/// <summary>
	///     Verifies the interactions with the mocked subject of <typeparamref name="T" />.
	/// </summary>
	public T Verify
	{
		get
		{
			UpdateInteractions();
			return field;
		}
	}
}
