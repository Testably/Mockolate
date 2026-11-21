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
