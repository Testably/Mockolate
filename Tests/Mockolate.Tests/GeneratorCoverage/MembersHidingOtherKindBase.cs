namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Hides a virtual method with a virtual property of the same name, so the mock cannot override the method.
/// </summary>
public class MembersHidingOtherKindBase : MembersHiddenByOtherKindBase
{
	public new virtual int Run { get; set; }
}
