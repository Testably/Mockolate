using System;

namespace Mockolate.Frameworks.TestHelpers.Domain;

public sealed class ChocolateFactory
{
	public ChocolateFactory() : this(o => o, _ => { }) { }

	public ChocolateFactory(Func<ChocolateFactoryOptions, ChocolateFactoryOptions> options,
		Action<ChocolateFactoryOptions> configure)
	{
		Options = options(new ChocolateFactoryOptions());
		configure(Options);
	}

	public ChocolateFactoryOptions Options { get; }
}

public sealed class ChocolateFactoryOptions
{
	public string Flavor { get; set; } = "Dark";
}
