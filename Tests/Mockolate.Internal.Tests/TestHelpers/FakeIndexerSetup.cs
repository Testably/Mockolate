using Mockolate.Interactions;
using Mockolate.Setup;

namespace Mockolate.Internal.Tests.TestHelpers;

internal sealed class FakeIndexerSetup : IndexerSetup
{
	private readonly Action? _onMatch;

	internal FakeIndexerSetup(bool match, Action? onMatch = null)
		: base(new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)))
	{
		ShouldMatch = match;
		_onMatch = onMatch;
	}

	internal bool ShouldMatch { get; }

	public static bool InvokeTryCast<T>(object? value, out T result, MockBehavior behavior)
		=> TryCast(value, out result, behavior);

	protected override bool MatchesAccess(IndexerAccess access)
	{
		_onMatch?.Invoke();
		return ShouldMatch;
	}

	public override bool? SkipBaseClass() => null;

	public override TResult GetResult<TResult>(IndexerAccess access, MockBehavior behavior, TResult baseValue)
		=> baseValue;

	public override TResult GetResult<TResult>(IndexerAccess access, MockBehavior behavior)
		=> default!;

	public override TResult GetResult<TResult>(IndexerAccess access, MockBehavior behavior,
		Func<TResult> defaultValueGenerator)
		=> defaultValueGenerator();

	public override void SetResult<TResult>(IndexerAccess access, MockBehavior behavior, TResult value)
	{
	}
}
