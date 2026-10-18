using System;

namespace Mockolate.Setup;

/// <summary>
///     Extension methods for <see cref="Callbacks{T}" />.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
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
	}
}
