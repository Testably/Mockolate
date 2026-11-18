using System.Reflection;
using Mockolate.Interactions;

namespace Mockolate.Internal.Tests;

internal static class RecordViewExtensions
{
	extension(MethodRecords view)
	{
		public void Append(string name)
			=> MethodRecords.Append(view.Owner, view.MemberId, name);
	}

	extension<T1>(MethodRecords<T1> view)
	{
		public void Append(string name, T1 parameter1)
			=> MethodRecords<T1>.Append(view.Owner, view.MemberId, name, parameter1);
	}

	extension<T1, T2>(MethodRecords<T1, T2> view)
	{
		public void Append(string name, T1 parameter1, T2 parameter2)
			=> MethodRecords<T1, T2>.Append(view.Owner, view.MemberId, name, parameter1, parameter2);
	}

	extension<T1, T2, T3>(MethodRecords<T1, T2, T3> view)
	{
		public void Append(string name, T1 parameter1, T2 parameter2, T3 parameter3)
			=> MethodRecords<T1, T2, T3>.Append(view.Owner, view.MemberId, name, parameter1, parameter2,
				parameter3);
	}

	extension<T1, T2, T3, T4>(MethodRecords<T1, T2, T3, T4> view)
	{
		public void Append(string name, T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4)
			=> MethodRecords<T1, T2, T3, T4>.Append(view.Owner, view.MemberId, name, parameter1, parameter2,
				parameter3, parameter4);
	}

	extension(PropertyGetterRecords view)
	{
		public void Append()
			=> PropertyGetterRecords.Append(view.Owner, view.MemberId, new PropertyGetterAccess(string.Empty));

		public void Append(PropertyGetterAccess access)
			=> PropertyGetterRecords.Append(view.Owner, view.MemberId, access);
	}

	extension<T>(PropertySetterRecords<T> view)
	{
		public void Append(string name, T value)
			=> PropertySetterRecords<T>.Append(view.Owner, view.MemberId, name, value);
	}

	extension(EventRecords view)
	{
		public void AppendSubscription(string name, object? target, MethodInfo method)
			=> EventRecords.Append(view.Owner, view.MemberId, EventRecordKind.Subscribe, name, target, method);

		public void AppendUnsubscription(string name, object? target, MethodInfo method)
			=> EventRecords.Append(view.Owner, view.MemberId, EventRecordKind.Unsubscribe, name, target,
				method);
	}

	extension<T1>(IndexerGetterRecords<T1> view)
	{
		public void Append(T1 parameter1)
			=> IndexerGetterRecords<T1>.Append(view.Owner, view.MemberId, parameter1);

		public void Append(IndexerGetterAccess<T1> access)
			=> IndexerGetterRecords<T1>.Append(view.Owner, view.MemberId, access);
	}

	extension<T1, T2>(IndexerGetterRecords<T1, T2> view)
	{
		public void Append(T1 parameter1, T2 parameter2)
			=> IndexerGetterRecords<T1, T2>.Append(view.Owner, view.MemberId, parameter1, parameter2);

		public void Append(IndexerGetterAccess<T1, T2> access)
			=> IndexerGetterRecords<T1, T2>.Append(view.Owner, view.MemberId, access);
	}

	extension<T1, T2, T3>(IndexerGetterRecords<T1, T2, T3> view)
	{
		public void Append(T1 parameter1, T2 parameter2, T3 parameter3)
			=> IndexerGetterRecords<T1, T2, T3>.Append(view.Owner, view.MemberId, parameter1, parameter2,
				parameter3);

		public void Append(IndexerGetterAccess<T1, T2, T3> access)
			=> IndexerGetterRecords<T1, T2, T3>.Append(view.Owner, view.MemberId, access);
	}

	extension<T1, T2, T3, T4>(IndexerGetterRecords<T1, T2, T3, T4> view)
	{
		public void Append(T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4)
			=> IndexerGetterRecords<T1, T2, T3, T4>.Append(view.Owner, view.MemberId, parameter1, parameter2,
				parameter3, parameter4);

		public void Append(IndexerGetterAccess<T1, T2, T3, T4> access)
			=> IndexerGetterRecords<T1, T2, T3, T4>.Append(view.Owner, view.MemberId, access);
	}

	extension<T1, TValue>(IndexerSetterRecords<T1, TValue> view)
	{
		public void Append(T1 parameter1, TValue value)
			=> IndexerSetterRecords<T1, TValue>.Append(view.Owner, view.MemberId, parameter1, value);

		public void Append(IndexerSetterAccess<T1, TValue> access)
			=> IndexerSetterRecords<T1, TValue>.Append(view.Owner, view.MemberId, access);
	}

	extension<T1, T2, TValue>(IndexerSetterRecords<T1, T2, TValue> view)
	{
		public void Append(T1 parameter1, T2 parameter2, TValue value)
			=> IndexerSetterRecords<T1, T2, TValue>.Append(view.Owner, view.MemberId, parameter1, parameter2,
				value);

		public void Append(IndexerSetterAccess<T1, T2, TValue> access)
			=> IndexerSetterRecords<T1, T2, TValue>.Append(view.Owner, view.MemberId, access);
	}

	extension<T1, T2, T3, TValue>(IndexerSetterRecords<T1, T2, T3, TValue> view)
	{
		public void Append(T1 parameter1, T2 parameter2, T3 parameter3, TValue value)
			=> IndexerSetterRecords<T1, T2, T3, TValue>.Append(view.Owner, view.MemberId, parameter1,
				parameter2, parameter3, value);

		public void Append(IndexerSetterAccess<T1, T2, T3, TValue> access)
			=> IndexerSetterRecords<T1, T2, T3, TValue>.Append(view.Owner, view.MemberId, access);
	}

	extension<T1, T2, T3, T4, TValue>(IndexerSetterRecords<T1, T2, T3, T4, TValue> view)
	{
		public void Append(T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4, TValue value)
			=> IndexerSetterRecords<T1, T2, T3, T4, TValue>.Append(view.Owner, view.MemberId, parameter1,
				parameter2, parameter3, parameter4, value);

		public void Append(IndexerSetterAccess<T1, T2, T3, T4, TValue> access)
			=> IndexerSetterRecords<T1, T2, T3, T4, TValue>.Append(view.Owner, view.MemberId, access);
	}
}
