using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using Mockolate.Interactions;

namespace Mockolate.Setup;

internal partial class MockSetups
{
	[DebuggerDisplay("{ToString()}")]
#if !DEBUG
	[DebuggerNonUserCode]
#endif
	internal sealed class MethodSetups
	{
		private List<MethodSetup>? _storage;

		public bool HasStorage => _storage is not null;

		public int Count
		{
			get
			{
				List<MethodSetup>? storage = _storage;
				if (storage is null)
				{
					return 0;
				}

				lock (storage)
				{
					return storage.Count;
				}
			}
		}

		private List<MethodSetup> GetOrCreateStorage()
		{
			if (_storage is null)
			{
				Interlocked.CompareExchange(ref _storage, [], null);
			}

			return _storage!;
		}

		public void Add(MethodSetup setup)
		{
			List<MethodSetup> storage = GetOrCreateStorage();
			lock (storage)
			{
				storage.Add(setup);
			}
		}

		/// <summary>
		///     Enumerates all method setups of type <typeparamref name="T" /> matching <paramref name="methodName" />
		///     in latest-registered-first order.
		/// </summary>
		/// <remarks>
		///     Used by the generated code for methods with ref-struct parameters, where the usual
		///     <c>GetMatching</c> predicate cannot capture the ref-struct value. Callers iterate this
		///     sequence and evaluate <c>Matches</c> synchronously on the stack, then invoke the first
		///     matching setup. The empty-storage path returns <see cref="Array.Empty{T}" />, so the dispatch
		///     hot path skips the snapshot allocation per call when no string-keyed setups have been
		///     registered (the common case after generator-emitted <c>SetupMethod(int, ...)</c> bypasses
		///     this list).
		/// </remarks>
		public IEnumerable<T> EnumerateByName<T>(string methodName, string? alternativeMethodName = null)
			where T : MethodSetup
		{
			List<MethodSetup>? storage = _storage;
			if (storage is null)
			{
				return Array.Empty<T>();
			}

			// Return a snapshot instead of iterating the storage so the caller's Matches/Invoke can run
			// user code (including throws) without holding or re-entering the storage lock.
			List<T> snapshot;
			lock (storage)
			{
				snapshot = new List<T>(storage.Count);
				for (int i = storage.Count - 1; i >= 0; i--)
				{
					if (storage[i].HasName(methodName, alternativeMethodName) && storage[i] is T typed)
					{
						snapshot.Add(typed);
					}
				}
			}

			return snapshot;
		}

		internal IEnumerable<MethodSetup> EnumerateUnusedSetupsBy(IMockInteractions interactions)
		{
			List<MethodSetup>? storage = _storage;
			if (storage is null)
			{
				return [];
			}

			lock (storage)
			{
				return storage.Where(methodSetup => !interactions.OfType<IMethodInteraction>()
						.Any(methodInvocation => ((IVerifiableMethodSetup)methodSetup).Matches(methodInvocation)))
					.ToList();
			}
		}

		/// <inheritdoc cref="object.ToString()" />
		[ExcludeFromCodeCoverage]
		public override string ToString()
		{
			List<MethodSetup>? storage = _storage;
			if (storage is null)
			{
				return "0 methods";
			}

			lock (storage)
			{
				int count = storage.Count;
				if (count == 0)
				{
					return "0 methods";
				}

				StringBuilder sb = new();
				sb.Append(count).Append(count == 1 ? " method:" : " methods:").AppendLine();
				foreach (MethodSetup methodSetup in storage)
				{
					sb.Append(methodSetup).AppendLine();
				}

				sb.Length -= Environment.NewLine.Length;
				return sb.ToString();
			}
		}
	}
}
