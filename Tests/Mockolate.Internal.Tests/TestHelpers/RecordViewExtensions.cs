using System.Reflection;
using Mockolate.Interactions;

namespace Mockolate.Internal.Tests;

internal static class RecordViewExtensions
{
	extension(FastMethod0Buffer view)
	{
		public void Append(string name)
			=> FastMethod0Buffer.Append(view.Owner, view.MemberId, name);
	}

	extension<T1>(FastMethod1Buffer<T1> view)
	{
		public void Append(string name, T1 parameter1)
			=> FastMethod1Buffer<T1>.Append(view.Owner, view.MemberId, name, parameter1);
	}

	extension<T1, T2>(FastMethod2Buffer<T1, T2> view)
	{
		public void Append(string name, T1 parameter1, T2 parameter2)
			=> FastMethod2Buffer<T1, T2>.Append(view.Owner, view.MemberId, name, parameter1, parameter2);
	}

	extension<T1, T2, T3>(FastMethod3Buffer<T1, T2, T3> view)
	{
		public void Append(string name, T1 parameter1, T2 parameter2, T3 parameter3)
			=> FastMethod3Buffer<T1, T2, T3>.Append(view.Owner, view.MemberId, name, parameter1, parameter2,
				parameter3);
	}

	extension<T1, T2, T3, T4>(FastMethod4Buffer<T1, T2, T3, T4> view)
	{
		public void Append(string name, T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4)
			=> FastMethod4Buffer<T1, T2, T3, T4>.Append(view.Owner, view.MemberId, name, parameter1, parameter2,
				parameter3, parameter4);
	}

	extension(FastPropertyGetterBuffer view)
	{
		public void Append()
			=> FastPropertyGetterBuffer.Append(view.Owner, view.MemberId, new PropertyGetterAccess(string.Empty));

		public void Append(PropertyGetterAccess access)
			=> FastPropertyGetterBuffer.Append(view.Owner, view.MemberId, access);
	}

	extension<T>(FastPropertySetterBuffer<T> view)
	{
		public void Append(string name, T value)
			=> FastPropertySetterBuffer<T>.Append(view.Owner, view.MemberId, name, value);
	}

	extension(FastEventBuffer view)
	{
		public void AppendSubscription(string name, object? target, MethodInfo method)
			=> FastEventBuffer.Append(view.Owner, view.MemberId, FastEventBufferKind.Subscribe, name, target, method);

		public void AppendUnsubscription(string name, object? target, MethodInfo method)
			=> FastEventBuffer.Append(view.Owner, view.MemberId, FastEventBufferKind.Unsubscribe, name, target,
				method);
	}

	extension<T1>(FastIndexerGetterBuffer<T1> view)
	{
		public void Append(T1 parameter1)
			=> FastIndexerGetterBuffer<T1>.Append(view.Owner, view.MemberId, parameter1);

		public void Append(IndexerGetterAccess<T1> access)
			=> FastIndexerGetterBuffer<T1>.Append(view.Owner, view.MemberId, access);
	}

	extension<T1, T2>(FastIndexerGetterBuffer<T1, T2> view)
	{
		public void Append(T1 parameter1, T2 parameter2)
			=> FastIndexerGetterBuffer<T1, T2>.Append(view.Owner, view.MemberId, parameter1, parameter2);

		public void Append(IndexerGetterAccess<T1, T2> access)
			=> FastIndexerGetterBuffer<T1, T2>.Append(view.Owner, view.MemberId, access);
	}

	extension<T1, T2, T3>(FastIndexerGetterBuffer<T1, T2, T3> view)
	{
		public void Append(T1 parameter1, T2 parameter2, T3 parameter3)
			=> FastIndexerGetterBuffer<T1, T2, T3>.Append(view.Owner, view.MemberId, parameter1, parameter2,
				parameter3);

		public void Append(IndexerGetterAccess<T1, T2, T3> access)
			=> FastIndexerGetterBuffer<T1, T2, T3>.Append(view.Owner, view.MemberId, access);
	}

	extension<T1, T2, T3, T4>(FastIndexerGetterBuffer<T1, T2, T3, T4> view)
	{
		public void Append(T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4)
			=> FastIndexerGetterBuffer<T1, T2, T3, T4>.Append(view.Owner, view.MemberId, parameter1, parameter2,
				parameter3, parameter4);

		public void Append(IndexerGetterAccess<T1, T2, T3, T4> access)
			=> FastIndexerGetterBuffer<T1, T2, T3, T4>.Append(view.Owner, view.MemberId, access);
	}

	extension<T1, TValue>(FastIndexerSetterBuffer<T1, TValue> view)
	{
		public void Append(T1 parameter1, TValue value)
			=> FastIndexerSetterBuffer<T1, TValue>.Append(view.Owner, view.MemberId, parameter1, value);

		public void Append(IndexerSetterAccess<T1, TValue> access)
			=> FastIndexerSetterBuffer<T1, TValue>.Append(view.Owner, view.MemberId, access);
	}

	extension<T1, T2, TValue>(FastIndexerSetterBuffer<T1, T2, TValue> view)
	{
		public void Append(T1 parameter1, T2 parameter2, TValue value)
			=> FastIndexerSetterBuffer<T1, T2, TValue>.Append(view.Owner, view.MemberId, parameter1, parameter2,
				value);

		public void Append(IndexerSetterAccess<T1, T2, TValue> access)
			=> FastIndexerSetterBuffer<T1, T2, TValue>.Append(view.Owner, view.MemberId, access);
	}

	extension<T1, T2, T3, TValue>(FastIndexerSetterBuffer<T1, T2, T3, TValue> view)
	{
		public void Append(T1 parameter1, T2 parameter2, T3 parameter3, TValue value)
			=> FastIndexerSetterBuffer<T1, T2, T3, TValue>.Append(view.Owner, view.MemberId, parameter1,
				parameter2, parameter3, value);

		public void Append(IndexerSetterAccess<T1, T2, T3, TValue> access)
			=> FastIndexerSetterBuffer<T1, T2, T3, TValue>.Append(view.Owner, view.MemberId, access);
	}

	extension<T1, T2, T3, T4, TValue>(FastIndexerSetterBuffer<T1, T2, T3, T4, TValue> view)
	{
		public void Append(T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4, TValue value)
			=> FastIndexerSetterBuffer<T1, T2, T3, T4, TValue>.Append(view.Owner, view.MemberId, parameter1,
				parameter2, parameter3, parameter4, value);

		public void Append(IndexerSetterAccess<T1, T2, T3, T4, TValue> access)
			=> FastIndexerSetterBuffer<T1, T2, T3, T4, TValue>.Append(view.Owner, view.MemberId, access);
	}
}
