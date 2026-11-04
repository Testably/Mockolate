using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Mockolate.Interactions;
using Mockolate.Parameters;

namespace Mockolate.Internals;

/// <summary>
///     Per-member buffer for parameterless methods. Records only the method name + sequence number.
/// </summary>
[DebuggerDisplay("{Count} method calls")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class FastMethod0Buffer : IFastMemberBuffer
{
	private readonly FastMockInteractions _owner;
	private readonly ChunkedSlotStorage<Record> _storage = new();

	/// <summary>
	///     Creates a new parameterless method buffer attached to <paramref name="owner" />.
	/// </summary>
	/// <param name="owner">The mock-wide <see cref="FastMockInteractions" /> the buffer publishes records into.</param>
	public FastMethod0Buffer(FastMockInteractions owner)
	{
		_owner = owner;
	}

	/// <inheritdoc cref="IFastMemberBuffer.Count" />
	public int Count => _storage.Count;

	/// <summary>
	///     Records a parameterless method call.
	/// </summary>
	public void Append(string name)
	{
		long seq = _owner.NextSequence();
		int slot = _storage.Reserve();
		ref Record r = ref _storage.SlotForWrite(slot);
		r.Seq = seq;
		r.Name = name;
		r.Boxed = null;
		_storage.Publish();

		if (_owner.HasInteractionAddedSubscribers)
		{
			_owner.RaiseAdded();
		}
	}

	/// <inheritdoc cref="IFastMemberBuffer.Clear" />
	public void Clear() => _storage.Clear();

	void IFastMemberBuffer.AppendBoxed(List<(long Seq, IInteraction Interaction)> dest)
	{
		lock (_storage.Lock)
		{
			int n = _storage.PublishedUnderLock;
			for (int slot = 0; slot < n; slot++)
			{
				ref Record r = ref _storage.SlotUnderLock(slot);
				r.Boxed ??= new MethodInvocation(r.Name);
				dest.Add((r.Seq, r.Boxed));
			}
		}
	}

	void IFastMemberBuffer.AppendBoxedUnverified(List<(long Seq, IInteraction Interaction)> dest)
	{
		lock (_storage.Lock)
		{
			int n = _storage.PublishedUnderLock;
			for (int slot = 0; slot < n; slot++)
			{
				if (_storage.VerifiedUnderLock(slot))
				{
					continue;
				}

				ref Record r = ref _storage.SlotUnderLock(slot);
				r.Boxed ??= new MethodInvocation(r.Name);
				dest.Add((r.Seq, r.Boxed));
			}
		}
	}

	/// <summary>
	///     Returns the number of recorded calls and marks every currently-published slot as verified
	///     so a subsequent <see cref="IMockInteractions.GetUnverifiedInteractions" /> walk skips them.
	///     Parameterless overload of the typed
	///     <see cref="FastMethod1Buffer{T1}.ConsumeMatching(IParameterMatch{T1})" /> family — provided so
	///     count-only verification can dispatch uniformly across arities without allocating a boxed
	///     <see cref="IInteraction" /> per recorded call. The name reflects the side effect: this is a
	///     <c>Count</c> + <c>MarkVerified</c> step, not a pure read.
	/// </summary>
	public int ConsumeMatching()
	{
		lock (_storage.Lock)
		{
			int n = _storage.PublishedUnderLock;
			for (int slot = 0; slot < n; slot++)
			{
				_storage.VerifiedUnderLock(slot) = true;
			}

			return n;
		}
	}

	internal struct Record
	{
		public long Seq;
		public string Name;
		public IInteraction? Boxed;
	}
}

/// <summary>
///     Per-member buffer for 1-parameter methods.
/// </summary>
[DebuggerDisplay("{Count} method calls")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class FastMethod1Buffer<T1> : IFastMemberBuffer
{
	private readonly FastMockInteractions _owner;
	private readonly ChunkedSlotStorage<Record> _storage = new();

	/// <summary>
	///     Creates a new 1-parameter method buffer attached to <paramref name="owner" />.
	/// </summary>
	/// <param name="owner">The mock-wide <see cref="FastMockInteractions" /> the buffer publishes records into.</param>
	public FastMethod1Buffer(FastMockInteractions owner)
	{
		_owner = owner;
	}

	/// <inheritdoc cref="IFastMemberBuffer.Count" />
	public int Count => _storage.Count;

	/// <summary>
	///     Records a 1-parameter method call.
	/// </summary>
	public void Append(string name, T1 parameter1)
	{
		long seq = _owner.NextSequence();
		int slot = _storage.Reserve();
		ref Record r = ref _storage.SlotForWrite(slot);
		r.Seq = seq;
		r.Name = name;
		r.P1 = parameter1;
		r.Boxed = null;
		_storage.Publish();

		if (_owner.HasInteractionAddedSubscribers)
		{
			_owner.RaiseAdded();
		}
	}

	/// <inheritdoc cref="IFastMemberBuffer.Clear" />
	public void Clear() => _storage.Clear();

	void IFastMemberBuffer.AppendBoxed(List<(long Seq, IInteraction Interaction)> dest)
	{
		lock (_storage.Lock)
		{
			int n = _storage.PublishedUnderLock;
			for (int slot = 0; slot < n; slot++)
			{
				ref Record r = ref _storage.SlotUnderLock(slot);
				r.Boxed ??= new MethodInvocation<T1>(r.Name, r.P1);
				dest.Add((r.Seq, r.Boxed));
			}
		}
	}

	void IFastMemberBuffer.AppendBoxedUnverified(List<(long Seq, IInteraction Interaction)> dest)
	{
		lock (_storage.Lock)
		{
			int n = _storage.PublishedUnderLock;
			for (int slot = 0; slot < n; slot++)
			{
				if (_storage.VerifiedUnderLock(slot))
				{
					continue;
				}

				ref Record r = ref _storage.SlotUnderLock(slot);
				r.Boxed ??= new MethodInvocation<T1>(r.Name, r.P1);
				dest.Add((r.Seq, r.Boxed));
			}
		}
	}

	/// <summary>
	///     Counts recorded calls whose parameter satisfies <paramref name="match1" />, marking each
	///     matched slot as verified so it is skipped by a later
	///     <see cref="IMockInteractions.GetUnverifiedInteractions" /> walk. Walks the typed storage in
	///     place and never allocates an <see cref="IInteraction" />, so count-only verification is
	///     allocation-free in the common case.
	/// </summary>
	public int ConsumeMatching(IParameterMatch<T1> match1)
	{
		int matches = 0;
		lock (_storage.Lock)
		{
			int n = _storage.PublishedUnderLock;
			for (int slot = 0; slot < n; slot++)
			{
				ref Record r = ref _storage.SlotUnderLock(slot);
				if (match1.Matches(r.P1))
				{
					matches++;
					_storage.VerifiedUnderLock(slot) = true;
				}
			}
		}

		return matches;
	}

	/// <summary>
	///     Literal-value variant of <see cref="ConsumeMatching(IParameterMatch{T1})" /> — compares each
	///     recorded parameter against <paramref name="value1" /> using
	///     <see cref="EqualityComparer{T}.Default" />, sparing the per-call <see cref="IParameterMatch{T1}" />
	///     allocation that the matcher overload requires.
	/// </summary>
	public int ConsumeMatchingLiteral(T1 value1)
	{
		int matches = 0;
		EqualityComparer<T1> cmp = EqualityComparer<T1>.Default;
		lock (_storage.Lock)
		{
			int n = _storage.PublishedUnderLock;
			for (int slot = 0; slot < n; slot++)
			{
				ref Record r = ref _storage.SlotUnderLock(slot);
				if (cmp.Equals(value1, r.P1))
				{
					matches++;
					_storage.VerifiedUnderLock(slot) = true;
				}
			}
		}

		return matches;
	}

	internal int ConsumeAll()
	{
		lock (_storage.Lock)
		{
			int n = _storage.PublishedUnderLock;
			for (int slot = 0; slot < n; slot++)
			{
				_storage.VerifiedUnderLock(slot) = true;
			}

			return n;
		}
	}

	internal struct Record
	{
		public long Seq;
		public string Name;
		public T1 P1;
		public IInteraction? Boxed;
	}
}

/// <summary>
///     Per-member buffer for 2-parameter methods.
/// </summary>
[DebuggerDisplay("{Count} method calls")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class FastMethod2Buffer<T1, T2> : IFastMemberBuffer
{
	private readonly FastMockInteractions _owner;
	private readonly ChunkedSlotStorage<Record> _storage = new();

	/// <summary>
	///     Creates a new 2-parameter method buffer attached to <paramref name="owner" />.
	/// </summary>
	/// <param name="owner">The mock-wide <see cref="FastMockInteractions" /> the buffer publishes records into.</param>
	public FastMethod2Buffer(FastMockInteractions owner)
	{
		_owner = owner;
	}

	/// <inheritdoc cref="IFastMemberBuffer.Count" />
	public int Count => _storage.Count;

	/// <summary>
	///     Records a 2-parameter method call.
	/// </summary>
	public void Append(string name, T1 parameter1, T2 parameter2)
	{
		long seq = _owner.NextSequence();
		int slot = _storage.Reserve();
		ref Record r = ref _storage.SlotForWrite(slot);
		r.Seq = seq;
		r.Name = name;
		r.P1 = parameter1;
		r.P2 = parameter2;
		r.Boxed = null;
		_storage.Publish();

		if (_owner.HasInteractionAddedSubscribers)
		{
			_owner.RaiseAdded();
		}
	}

	/// <inheritdoc cref="IFastMemberBuffer.Clear" />
	public void Clear() => _storage.Clear();

	void IFastMemberBuffer.AppendBoxed(List<(long Seq, IInteraction Interaction)> dest)
	{
		lock (_storage.Lock)
		{
			int n = _storage.PublishedUnderLock;
			for (int slot = 0; slot < n; slot++)
			{
				ref Record r = ref _storage.SlotUnderLock(slot);
				r.Boxed ??= new MethodInvocation<T1, T2>(r.Name, r.P1, r.P2);
				dest.Add((r.Seq, r.Boxed));
			}
		}
	}

	void IFastMemberBuffer.AppendBoxedUnverified(List<(long Seq, IInteraction Interaction)> dest)
	{
		lock (_storage.Lock)
		{
			int n = _storage.PublishedUnderLock;
			for (int slot = 0; slot < n; slot++)
			{
				if (_storage.VerifiedUnderLock(slot))
				{
					continue;
				}

				ref Record r = ref _storage.SlotUnderLock(slot);
				r.Boxed ??= new MethodInvocation<T1, T2>(r.Name, r.P1, r.P2);
				dest.Add((r.Seq, r.Boxed));
			}
		}
	}

	/// <summary>
	///     Counts recorded calls whose parameters satisfy the supplied matchers, marking each matched
	///     slot as verified. Allocation-free fast path for count-only verification; mirrors
	///     <see cref="FastMethod1Buffer{T1}.ConsumeMatching(IParameterMatch{T1})" />.
	/// </summary>
	public int ConsumeMatching(IParameterMatch<T1> match1, IParameterMatch<T2> match2)
	{
		int matches = 0;
		lock (_storage.Lock)
		{
			int n = _storage.PublishedUnderLock;
			for (int slot = 0; slot < n; slot++)
			{
				ref Record r = ref _storage.SlotUnderLock(slot);
				if (match1.Matches(r.P1) && match2.Matches(r.P2))
				{
					matches++;
					_storage.VerifiedUnderLock(slot) = true;
				}
			}
		}

		return matches;
	}

	/// <summary>
	///     Literal-value variant of
	///     <see cref="ConsumeMatching(IParameterMatch{T1}, IParameterMatch{T2})" /> — compares each
	///     recorded parameter against <paramref name="value1" /> / <paramref name="value2" /> using
	///     <see cref="EqualityComparer{T}.Default" />, sparing the per-call matcher allocations.
	/// </summary>
	public int ConsumeMatchingLiteral(T1 value1, T2 value2)
	{
		int matches = 0;
		EqualityComparer<T1> cmp1 = EqualityComparer<T1>.Default;
		EqualityComparer<T2> cmp2 = EqualityComparer<T2>.Default;
		lock (_storage.Lock)
		{
			int n = _storage.PublishedUnderLock;
			for (int slot = 0; slot < n; slot++)
			{
				ref Record r = ref _storage.SlotUnderLock(slot);
				if (cmp1.Equals(value1, r.P1) && cmp2.Equals(value2, r.P2))
				{
					matches++;
					_storage.VerifiedUnderLock(slot) = true;
				}
			}
		}

		return matches;
	}

	internal int ConsumeAll()
	{
		lock (_storage.Lock)
		{
			int n = _storage.PublishedUnderLock;
			for (int slot = 0; slot < n; slot++)
			{
				_storage.VerifiedUnderLock(slot) = true;
			}

			return n;
		}
	}

	internal struct Record
	{
		public long Seq;
		public string Name;
		public T1 P1;
		public T2 P2;
		public IInteraction? Boxed;
	}
}

/// <summary>
///     Per-member buffer for 3-parameter methods.
/// </summary>
[DebuggerDisplay("{Count} method calls")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class FastMethod3Buffer<T1, T2, T3> : IFastMemberBuffer
{
	private readonly FastMockInteractions _owner;
	private readonly ChunkedSlotStorage<Record> _storage = new();

	/// <summary>
	///     Creates a new 3-parameter method buffer attached to <paramref name="owner" />.
	/// </summary>
	/// <param name="owner">The mock-wide <see cref="FastMockInteractions" /> the buffer publishes records into.</param>
	public FastMethod3Buffer(FastMockInteractions owner)
	{
		_owner = owner;
	}

	/// <inheritdoc cref="IFastMemberBuffer.Count" />
	public int Count => _storage.Count;

	/// <summary>
	///     Records a 3-parameter method call.
	/// </summary>
	public void Append(string name, T1 parameter1, T2 parameter2, T3 parameter3)
	{
		long seq = _owner.NextSequence();
		int slot = _storage.Reserve();
		ref Record r = ref _storage.SlotForWrite(slot);
		r.Seq = seq;
		r.Name = name;
		r.P1 = parameter1;
		r.P2 = parameter2;
		r.P3 = parameter3;
		r.Boxed = null;
		_storage.Publish();

		if (_owner.HasInteractionAddedSubscribers)
		{
			_owner.RaiseAdded();
		}
	}

	/// <inheritdoc cref="IFastMemberBuffer.Clear" />
	public void Clear() => _storage.Clear();

	void IFastMemberBuffer.AppendBoxed(List<(long Seq, IInteraction Interaction)> dest)
	{
		lock (_storage.Lock)
		{
			int n = _storage.PublishedUnderLock;
			for (int slot = 0; slot < n; slot++)
			{
				ref Record r = ref _storage.SlotUnderLock(slot);
				r.Boxed ??= new MethodInvocation<T1, T2, T3>(r.Name, r.P1, r.P2, r.P3);
				dest.Add((r.Seq, r.Boxed));
			}
		}
	}

	void IFastMemberBuffer.AppendBoxedUnverified(List<(long Seq, IInteraction Interaction)> dest)
	{
		lock (_storage.Lock)
		{
			int n = _storage.PublishedUnderLock;
			for (int slot = 0; slot < n; slot++)
			{
				if (_storage.VerifiedUnderLock(slot))
				{
					continue;
				}

				ref Record r = ref _storage.SlotUnderLock(slot);
				r.Boxed ??= new MethodInvocation<T1, T2, T3>(r.Name, r.P1, r.P2, r.P3);
				dest.Add((r.Seq, r.Boxed));
			}
		}
	}

	/// <summary>
	///     Counts recorded calls whose parameters satisfy the supplied matchers, marking each matched
	///     slot as verified. Allocation-free fast path for count-only verification.
	/// </summary>
	public int ConsumeMatching(IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3)
	{
		int matches = 0;
		lock (_storage.Lock)
		{
			int n = _storage.PublishedUnderLock;
			for (int slot = 0; slot < n; slot++)
			{
				ref Record r = ref _storage.SlotUnderLock(slot);
				if (match1.Matches(r.P1) && match2.Matches(r.P2) && match3.Matches(r.P3))
				{
					matches++;
					_storage.VerifiedUnderLock(slot) = true;
				}
			}
		}

		return matches;
	}

	/// <summary>
	///     Literal-value variant of
	///     <see cref="ConsumeMatching(IParameterMatch{T1}, IParameterMatch{T2}, IParameterMatch{T3})" />
	///     — compares each recorded parameter against the supplied values using
	///     <see cref="EqualityComparer{T}.Default" />, sparing the per-call matcher allocations.
	/// </summary>
	public int ConsumeMatchingLiteral(T1 value1, T2 value2, T3 value3)
	{
		int matches = 0;
		EqualityComparer<T1> cmp1 = EqualityComparer<T1>.Default;
		EqualityComparer<T2> cmp2 = EqualityComparer<T2>.Default;
		EqualityComparer<T3> cmp3 = EqualityComparer<T3>.Default;
		lock (_storage.Lock)
		{
			int n = _storage.PublishedUnderLock;
			for (int slot = 0; slot < n; slot++)
			{
				ref Record r = ref _storage.SlotUnderLock(slot);
				if (cmp1.Equals(value1, r.P1) && cmp2.Equals(value2, r.P2) && cmp3.Equals(value3, r.P3))
				{
					matches++;
					_storage.VerifiedUnderLock(slot) = true;
				}
			}
		}

		return matches;
	}

	internal int ConsumeAll()
	{
		lock (_storage.Lock)
		{
			int n = _storage.PublishedUnderLock;
			for (int slot = 0; slot < n; slot++)
			{
				_storage.VerifiedUnderLock(slot) = true;
			}

			return n;
		}
	}

	internal struct Record
	{
		public long Seq;
		public string Name;
		public T1 P1;
		public T2 P2;
		public T3 P3;
		public IInteraction? Boxed;
	}
}

/// <summary>
///     Per-member buffer for 4-parameter methods.
/// </summary>
[DebuggerDisplay("{Count} method calls")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class FastMethod4Buffer<T1, T2, T3, T4> : IFastMemberBuffer
{
	private readonly FastMockInteractions _owner;
	private readonly ChunkedSlotStorage<Record> _storage = new();

	/// <summary>
	///     Creates a new 4-parameter method buffer attached to <paramref name="owner" />.
	/// </summary>
	/// <param name="owner">The mock-wide <see cref="FastMockInteractions" /> the buffer publishes records into.</param>
	public FastMethod4Buffer(FastMockInteractions owner)
	{
		_owner = owner;
	}

	/// <inheritdoc cref="IFastMemberBuffer.Count" />
	public int Count => _storage.Count;

	/// <summary>
	///     Records a 4-parameter method call.
	/// </summary>
	public void Append(string name, T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4)
	{
		long seq = _owner.NextSequence();
		int slot = _storage.Reserve();
		ref Record r = ref _storage.SlotForWrite(slot);
		r.Seq = seq;
		r.Name = name;
		r.P1 = parameter1;
		r.P2 = parameter2;
		r.P3 = parameter3;
		r.P4 = parameter4;
		r.Boxed = null;
		_storage.Publish();

		if (_owner.HasInteractionAddedSubscribers)
		{
			_owner.RaiseAdded();
		}
	}

	/// <inheritdoc cref="IFastMemberBuffer.Clear" />
	public void Clear() => _storage.Clear();

	void IFastMemberBuffer.AppendBoxed(List<(long Seq, IInteraction Interaction)> dest)
	{
		lock (_storage.Lock)
		{
			int n = _storage.PublishedUnderLock;
			for (int slot = 0; slot < n; slot++)
			{
				ref Record r = ref _storage.SlotUnderLock(slot);
				r.Boxed ??= new MethodInvocation<T1, T2, T3, T4>(r.Name, r.P1, r.P2, r.P3, r.P4);
				dest.Add((r.Seq, r.Boxed));
			}
		}
	}

	void IFastMemberBuffer.AppendBoxedUnverified(List<(long Seq, IInteraction Interaction)> dest)
	{
		lock (_storage.Lock)
		{
			int n = _storage.PublishedUnderLock;
			for (int slot = 0; slot < n; slot++)
			{
				if (_storage.VerifiedUnderLock(slot))
				{
					continue;
				}

				ref Record r = ref _storage.SlotUnderLock(slot);
				r.Boxed ??= new MethodInvocation<T1, T2, T3, T4>(r.Name, r.P1, r.P2, r.P3, r.P4);
				dest.Add((r.Seq, r.Boxed));
			}
		}
	}

	/// <summary>
	///     Counts recorded calls whose parameters satisfy the supplied matchers, marking each matched
	///     slot as verified. Allocation-free fast path for count-only verification.
	/// </summary>
	public int ConsumeMatching(IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3, IParameterMatch<T4> match4)
	{
		int matches = 0;
		lock (_storage.Lock)
		{
			int n = _storage.PublishedUnderLock;
			for (int slot = 0; slot < n; slot++)
			{
				ref Record r = ref _storage.SlotUnderLock(slot);
				if (match1.Matches(r.P1) && match2.Matches(r.P2) && match3.Matches(r.P3) && match4.Matches(r.P4))
				{
					matches++;
					_storage.VerifiedUnderLock(slot) = true;
				}
			}
		}

		return matches;
	}

	/// <summary>
	///     Literal-value variant of
	///     <see cref="ConsumeMatching(IParameterMatch{T1}, IParameterMatch{T2}, IParameterMatch{T3}, IParameterMatch{T4})" />
	///     — compares each recorded parameter against the supplied values using
	///     <see cref="EqualityComparer{T}.Default" />, sparing the per-call matcher allocations.
	/// </summary>
	public int ConsumeMatchingLiteral(T1 value1, T2 value2, T3 value3, T4 value4)
	{
		int matches = 0;
		EqualityComparer<T1> cmp1 = EqualityComparer<T1>.Default;
		EqualityComparer<T2> cmp2 = EqualityComparer<T2>.Default;
		EqualityComparer<T3> cmp3 = EqualityComparer<T3>.Default;
		EqualityComparer<T4> cmp4 = EqualityComparer<T4>.Default;
		lock (_storage.Lock)
		{
			int n = _storage.PublishedUnderLock;
			for (int slot = 0; slot < n; slot++)
			{
				ref Record r = ref _storage.SlotUnderLock(slot);
				if (cmp1.Equals(value1, r.P1) && cmp2.Equals(value2, r.P2) &&
				    cmp3.Equals(value3, r.P3) && cmp4.Equals(value4, r.P4))
				{
					matches++;
					_storage.VerifiedUnderLock(slot) = true;
				}
			}
		}

		return matches;
	}

	internal int ConsumeAll()
	{
		lock (_storage.Lock)
		{
			int n = _storage.PublishedUnderLock;
			for (int slot = 0; slot < n; slot++)
			{
				_storage.VerifiedUnderLock(slot) = true;
			}

			return n;
		}
	}

	internal struct Record
	{
		public long Seq;
		public string Name;
		public T1 P1;
		public T2 P2;
		public T3 P3;
		public T4 P4;
		public IInteraction? Boxed;
	}
}
