using System;

namespace Mockolate.Tests.GeneratorCoverage;

public abstract class AbstractMemberBase
{
	public abstract int GetValue();
	public abstract string Name { get; set; }
	public abstract int this[int index] { get; }
	public abstract event EventHandler? Changed;
}

public class OverrideOfAbstractMember : AbstractMemberBase
{
	private EventHandler? _changed;
	private string _name = "base";

	public override event EventHandler? Changed
	{
		add => _changed += value;
		remove => _changed -= value;
	}

	public override string Name
	{
		get => _name;
		set => _name = value + "!";
	}

	public override int this[int index] => index * 2;

	public override int GetValue() => 42;

	public void RaiseChanged() => _changed?.Invoke(this, EventArgs.Empty);
}

public abstract class ReabstractedMember : OverrideOfAbstractMember
{
	public abstract override event EventHandler? Changed;
	public abstract override string Name { get; set; }
	public abstract override int GetValue();
}

public abstract class AbstractAccessorsBase
{
	public abstract string Value { get; set; }
	public abstract int this[int index] { get; set; }
}

public abstract class GetterOnlyOverride : AbstractAccessorsBase
{
	public override string Value => "override";
	public override int this[int index] => index * 2;
}

public abstract class SetterOnlyOverride : AbstractAccessorsBase
{
	public override string Value
	{
		set { }
	}

	public override int this[int index]
	{
		set { }
	}
}

public class VirtualMembersBase
{
	public virtual int this[int index]
	{
		get => index;
		set { }
	}

	public virtual string Name { get; set; } = "base";
	public virtual T Echo<T>(T value) => value;
}

public abstract class ReabstractedWithRenamedParameters : VirtualMembersBase
{
	public abstract override int this[int position] { get; set; }
	public abstract override U Echo<U>(U item);
}

public abstract class AbstractGenericMethodBase
{
	public abstract T Get<T>();
}

public abstract class ReabstractedGenericMethod : AbstractGenericMethodBase
{
	public abstract override U Get<U>();
}

public abstract class ReabstractedGetterOnly : VirtualMembersBase
{
	public abstract override string Name { get; }
}
