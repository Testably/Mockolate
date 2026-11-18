using System;
using System.ComponentModel;
using Mockolate.Setup;

namespace Mockolate.Internals;

/// <summary>
///     Extension methods for <see cref="Callbacks{T}" />.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public static class CallbacksExtensions
{
	/// <inheritdoc cref="CallbacksExtensions" />
	extension<T>(Callbacks<T>? callbacks) where T : Delegate
	{
		/// <summary>
		///     Appends the <paramref name="callback" /> to the <paramref name="callbacks" /> list
		///     and marks it as the currently active callback. Creates the list if it is <see langword="null" />.
		/// </summary>
		public Callbacks<T> Register(Callback<T> callback)
		{
			callbacks ??= [];
			callbacks.Active = callback;
			callbacks.Add(callback);
			return callbacks;
		}

		/// <summary>
		///     Appends the <paramref name="callback" /> to the <paramref name="callbacks" /> list as if it had
		///     already been invoked <paramref name="invocations" /> times, so that the sequence of the callbacks
		///     continues after a value that was returned without a callback.
		/// </summary>
		public Callbacks<T> Register(Callback<T> callback, int invocations)
		{
			callback.SkipInvocations(invocations);
			callbacks = callbacks.Register(callback);
			callbacks.CurrentIndex = invocations;
			return callbacks;
		}	}
}
