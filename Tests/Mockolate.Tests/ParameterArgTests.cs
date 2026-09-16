#if NET11_0_OR_GREATER
using Mockolate.Parameters;

namespace Mockolate.Tests;

// The generated ParameterArg<T> union is only emitted for this target (C# preview with MockolateUnionParameters
// enabled), so these tests exercise the real union conversions.
public sealed class ParameterArgTests
{
	[Fact]
	public async Task LiteralValue_ShouldConvertImplicitly()
	{
		ParameterArg<int> sut = 42;

		await That(sut.IsLiteral).IsTrue();
		await That(sut.HasValue).IsTrue();
		await That(sut.Literal).IsEqualTo(42);
		await That(sut.TryGetValue(out int literal)).IsTrue();
		await That(literal).IsEqualTo(42);
		await That(sut.TryGetValue(out IParameter<int>? _)).IsFalse();
		await That(sut.Value).IsEqualTo(42);
	}

	[Fact]
	public async Task TypedNullLiteral_ShouldBeTheLiteralCase()
	{
		string? value = null;

		ParameterArg<string> sut = value;

		await That(sut.IsLiteral).IsTrue();
		await That(sut.HasValue).IsTrue();
		await That(sut.Literal).IsNull();
		await That(sut.ToParameterMatch().Matches(null!)).IsTrue();
		await That(sut.ToParameterMatch().Matches("foo")).IsFalse();
	}

	[Fact]
	public async Task NullableValueTypeLiteral_ShouldBeTheLiteralCase()
	{
		ParameterArg<int?> sut = (int?)null;

		await That(sut.IsLiteral).IsTrue();
		await That(sut.HasValue).IsTrue();
		await That(sut.Literal).IsNull();
		await That(sut.ToParameterMatch().Matches(null)).IsTrue();
		await That(sut.ToParameterMatch().Matches(1)).IsFalse();
	}

	[Fact]
	public async Task Matcher_ShouldConvertImplicitly()
	{
		IParameter<int> matcher = It.IsInRange(1, 3);

		ParameterArg<int> sut = matcher;

		await That(sut.IsLiteral).IsFalse();
		await That(sut.HasValue).IsTrue();
		await That(sut.TryGetValue(out IParameter<int>? result)).IsTrue();
		await That(result).IsSameAs(matcher);
		await That(sut.TryGetValue(out int _)).IsFalse();
	}

	[Fact]
	public async Task CovariantMatcher_ShouldConvertToTheMatcherCase()
	{
		ParameterArg<object> sut = It.IsAny<string>();

		await That(sut.IsLiteral).IsFalse();
		await That(sut.ToParameterMatch().Matches("foo")).IsTrue();
		await That(sut.ToParameterMatch().Matches(42)).IsFalse();
	}

	[Fact]
	public async Task Default_ForReferenceType_ShouldBeTheLiteralNull()
	{
		ParameterArg<string> sut = default;

		await That(sut.HasValue).IsFalse();
		await That(sut.IsLiteral).IsTrue();
		await That(sut.Literal).IsNull();
		await That(sut.Value).IsNull();
		await That(sut.ToParameterMatch().Matches(null!)).IsTrue();
		await That(sut.ToParameterMatch().Matches("foo")).IsFalse();
	}

	[Fact]
	public async Task Default_ForValueType_ShouldBeTheLiteralDefaultValue()
	{
		ParameterArg<int> sut = default;

		await That(sut.HasValue).IsFalse();
		await That(sut.IsLiteral).IsTrue();
		await That(sut.Literal).IsEqualTo(0);
		await That(sut.Value).IsEqualTo(0);
		await That(sut.ToParameterMatch().Matches(0)).IsTrue();
		await That(sut.ToParameterMatch().Matches(1)).IsFalse();
	}

	[Fact]
	public async Task NullableParameter_ShouldAcceptNullAndBothCases()
	{
		static string Describe(ParameterArg<string>? arg)
			=> arg is null ? "none" : arg.Value.IsLiteral ? $"literal:{arg.Value.Literal}" : "matcher";

		await That(Describe(null)).IsEqualTo("none");
		await That(Describe("foo")).IsEqualTo("literal:foo");
		await That(Describe(It.IsAny<string>())).IsEqualTo("matcher");
	}

	[Fact]
	public async Task ToParameterMatch_ForLiteral_ShouldMatchOnEquality()
	{
		ParameterArg<int> sut = 5;

		IParameterMatch<int> result = sut.ToParameterMatch();

		await That(result.Matches(5)).IsTrue();
		await That(result.Matches(6)).IsFalse();
	}

	[Fact]
	public async Task ToParameterMatch_ForDirectMatcher_ShouldReturnTheSameInstance()
	{
		IParameter<int> matcher = It.IsAny<int>();
		ParameterArg<int> sut = matcher;

		IParameterMatch<int> result = sut.ToParameterMatch();

		await That(result).IsSameAs(matcher);
	}

	[Fact]
	public async Task ToParameterMatch_ForNullMatcher_ShouldMatchNull()
	{
		ParameterArg<string> sut = new((IParameter<string>)null!);

		IParameterMatch<string> result = sut.ToParameterMatch();

		await That(result.Matches(null!)).IsTrue();
		await That(result.Matches("foo")).IsFalse();
	}

	[Fact]
	public async Task ToString_ShouldDescribeTheContent()
	{
		ParameterArg<int> literal = 42;
		ParameterArg<int> matcher = It.IsAny<int>();
		ParameterArg<string> none = default;

		await That(literal.ToString()).IsEqualTo("42");
		await That(matcher.ToString()).IsEqualTo(It.IsAny<int>().ToString());
		await That(none.ToString()).IsEqualTo("null");
	}
}
#endif
