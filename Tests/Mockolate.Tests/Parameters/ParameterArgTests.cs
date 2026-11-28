#if NET11_0_OR_GREATER
using Mockolate.Parameters;

namespace Mockolate.Tests.Parameters;

// The generated ParameterArg<T> union is only emitted for this target (C# 15), so these tests exercise the real union conversions.
public sealed class ParameterArgTests
{
	[Test]
	public async Task WithLiteralValue_ShouldConvertImplicitly()
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

	[Test]
	public async Task WithTypedNullLiteral_ShouldBeTheLiteralCase()
	{
		string? value = null;

		ParameterArg<string> sut = value;

		await That(sut.IsLiteral).IsTrue();
		await That(sut.HasValue).IsFalse();
		await That(sut.TryGetValue(out string? _)).IsFalse();
		await That(sut.Literal).IsNull();
		await That(sut.ToParameterMatch().Matches(null!)).IsTrue();
		await That(sut.ToParameterMatch().Matches("foo")).IsFalse();
	}

	[Test]
	public async Task WithNullableValueTypeLiteral_ShouldBeTheLiteralCase()
	{
		ParameterArg<int?> sut = (int?)null;

		await That(sut.IsLiteral).IsTrue();
		await That(sut.HasValue).IsFalse();
		await That(sut.Literal).IsNull();
		await That(sut.ToParameterMatch().Matches(null)).IsTrue();
		await That(sut.ToParameterMatch().Matches(1)).IsFalse();
	}

	[Test]
	public async Task WithMatcher_ShouldConvertImplicitly()
	{
		IParameter<int> matcher = It.IsInRange(1, 3);

		ParameterArg<int> sut = matcher;

		await That(sut.IsLiteral).IsFalse();
		await That(sut.HasValue).IsTrue();
		await That(sut.TryGetValue(out IParameter<int>? result)).IsTrue();
		await That(result).IsSameAs(matcher);
		await That(sut.TryGetValue(out int _)).IsFalse();
	}

	[Test]
	public async Task WithCovariantMatcher_ShouldConvertToTheMatcherCase()
	{
		ParameterArg<object> sut = It.IsAny<string>();

		await That(sut.IsLiteral).IsFalse();
		await That(sut.ToParameterMatch().Matches("foo")).IsTrue();
		await That(sut.ToParameterMatch().Matches(42)).IsFalse();
	}

	[Test]
	public async Task WithDefaultOfReferenceType_ShouldBeTheLiteralNull()
	{
		ParameterArg<string> sut = default;

		await That(sut.HasValue).IsFalse();
		await That(sut.IsLiteral).IsTrue();
		await That(sut.Literal).IsNull();
		await That(sut.Value).IsNull();
		await That(sut.TryGetValue(out string? _)).IsFalse();
		await That(sut.TryGetValue(out IParameter<string>? _)).IsFalse();
		await That(sut.ToParameterMatch().Matches(null!)).IsTrue();
		await That(sut.ToParameterMatch().Matches("foo")).IsFalse();
	}

	[Test]
	public async Task WithDefaultOfValueType_ShouldBeTheLiteralDefaultValue()
	{
		ParameterArg<int> sut = default;

		await That(sut.HasValue).IsTrue().Because("the default instance is the literal 0, so Value is not null");
		await That(sut.IsLiteral).IsTrue();
		await That(sut.Literal).IsEqualTo(0);
		await That(sut.Value).IsEqualTo(0);
		await That(sut.TryGetValue(out int literal)).IsTrue();
		await That(literal).IsEqualTo(0);
		await That(sut.TryGetValue(out IParameter<int>? _)).IsFalse();
		await That(sut.ToParameterMatch().Matches(0)).IsTrue();
		await That(sut.ToParameterMatch().Matches(1)).IsFalse();
	}

	[Test]
	public async Task WithNullableType_ShouldAcceptNullAndBothCases()
	{
		static string Describe(ParameterArg<string>? arg)
			=> arg is null ? "none" : arg.Value.IsLiteral ? $"literal:{arg.Value.Literal}" : "matcher";

		await That(Describe(null)).IsEqualTo("none");
		await That(Describe("foo")).IsEqualTo("literal:foo");
		await That(Describe(It.IsAny<string>())).IsEqualTo("matcher");
	}

	[Test]
	public async Task ToParameterMatch_WithLiteral_ShouldMatchOnEquality()
	{
		ParameterArg<int> sut = 5;

		IParameterMatch<int> result = sut.ToParameterMatch();

		await That(result.Matches(5)).IsTrue();
		await That(result.Matches(6)).IsFalse();
	}

	[Test]
	public async Task ToParameterMatch_WithMatcher_ShouldReturnTheSameInstance()
	{
		IParameter<int> matcher = It.IsAny<int>();
		ParameterArg<int> sut = matcher;

		IParameterMatch<int> result = sut.ToParameterMatch();

		await That(result).IsSameAs(matcher);
	}

	[Test]
	public async Task ToParameterMatch_WithNullMatcher_ShouldMatchNull()
	{
		ParameterArg<string> sut = new((IParameter<string>)null!);

		IParameterMatch<string> result = sut.ToParameterMatch();

		await That(sut.HasValue).IsFalse();
		await That(sut.TryGetValue(out IParameter<string>? _)).IsFalse();
		await That(result.Matches(null!)).IsTrue();
		await That(result.Matches("foo")).IsFalse();
	}

	[Test]
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
