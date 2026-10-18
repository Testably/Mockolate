using System.Collections.Generic;
using Mockolate.Interactions;
using Mockolate.Parameters;
using Mockolate.Setup;

namespace Mockolate.Internal.Tests.Setup;

public sealed class MethodSetupMatchesTests
{
	private static MockRegistry CreateRegistry()
		=> new(MockBehavior.Default, new FastMockInteractions(0));

	private sealed class RecordingParametersMatch(bool result) : IParameters, IParametersMatch
	{
		public List<object?[]> Received { get; } = new();

		public bool Matches(ReadOnlySpan<object?> values)
		{
			Received.Add(values.ToArray());
			return result;
		}
	}

	private sealed class RecordingNamedParametersMatch(bool result) : IParameters, INamedParametersMatch
	{
		public List<(string, object?)[]> Received { get; } = new();

		public bool Matches(ReadOnlySpan<(string, object?)> values)
		{
			Received.Add(values.ToArray());
			return result;
		}
	}

	public sealed class ReturnMethodSetupTests
	{
		public sealed class OneParameter
		{
			[Test]
			public async Task Matches_WithINamedParametersMatch_ShouldForwardNamedParameterValue()
			{
				RecordingNamedParametersMatch matcher = new(true);
				ReturnMethodSetup<int, string>.WithParameters setup =
					new(CreateRegistry(), "M", matcher, "p1");

				bool result = setup.Matches("hello");

				await That(result).IsTrue();
				await That(matcher.Received).HasCount(1);
				await That(matcher.Received[0]).IsEqualTo(new[]
				{
					("p1", (object?)"hello"),
				});
			}

			[Test]
			public async Task Matches_WithIParametersMatch_ShouldForwardSingleParameterValue()
			{
				RecordingParametersMatch matcher = new(true);
				ReturnMethodSetup<int, string>.WithParameters setup =
					new(CreateRegistry(), "M", matcher, "p1");

				bool result = setup.Matches("hello");

				await That(result).IsTrue();
				await That(matcher.Received).HasCount(1);
				await That(matcher.Received[0]).IsEqualTo(new object?[]
				{
					"hello",
				});
			}

			[Test]
			public async Task Matches_WithUnknownIParameters_ShouldReturnTrue()
			{
				ReturnMethodSetup<int, string>.WithParameters setup =
					new(CreateRegistry(), "M", Match.AnyParameters(), "p1");

				bool result = setup.Matches("anything");

				await That(result).IsTrue();
			}

			[Test]
			public async Task MatchesInteraction_WithNonMethodInvocationOfT1_ShouldReturnFalse()
			{
				ReturnMethodSetup<int, string>.WithParameters setup =
					new(CreateRegistry(), "M", Match.AnyParameters(), "p1");

				bool result = ((IVerifiableMethodSetup)setup).Matches(new MethodInvocation("M"));

				await That(result).IsFalse();
			}
		}

		public sealed class TwoParameters
		{
			[Test]
			public async Task Matches_WithINamedParametersMatch_ShouldForwardBothNamedParameterValues()
			{
				RecordingNamedParametersMatch matcher = new(true);
				ReturnMethodSetup<int, string, int>.WithParameters setup =
					new(CreateRegistry(), "M", matcher, "p1", "p2");

				bool result = setup.Matches("a", 1);

				await That(result).IsTrue();
				await That(matcher.Received).HasCount(1);
				await That(matcher.Received[0]).IsEqualTo(new[]
				{
					("p1", "a"), ("p2", (object?)1),
				});
			}

			[Test]
			public async Task Matches_WithIParametersMatch_ShouldForwardBothParameterValues()
			{
				RecordingParametersMatch matcher = new(true);
				ReturnMethodSetup<int, string, int>.WithParameters setup =
					new(CreateRegistry(), "M", matcher, "p1", "p2");

				bool result = setup.Matches("a", 1);

				await That(result).IsTrue();
				await That(matcher.Received).HasCount(1);
				await That(matcher.Received[0]).IsEqualTo(new object?[]
				{
					"a", 1,
				});
			}

			[Test]
			public async Task Matches_WithUnknownIParameters_ShouldReturnTrue()
			{
				ReturnMethodSetup<int, string, int>.WithParameters setup =
					new(CreateRegistry(), "M", Match.AnyParameters(), "p1", "p2");

				bool result = setup.Matches("a", 1);

				await That(result).IsTrue();
			}

			[Test]
			public async Task MatchesInteraction_WithNonMethodInvocationOfT1AndT2_ShouldReturnFalse()
			{
				ReturnMethodSetup<int, string, int>.WithParameters setup =
					new(CreateRegistry(), "M", Match.AnyParameters(), "p1", "p2");

				bool result = ((IVerifiableMethodSetup)setup).Matches(new MethodInvocation("M"));

				await That(result).IsFalse();
			}
		}

		public sealed class ThreeParameters
		{
			[Test]
			public async Task Matches_WithINamedParametersMatch_ShouldForwardAllNamedParameterValues()
			{
				RecordingNamedParametersMatch matcher = new(true);
				ReturnMethodSetup<int, string, int, double>.WithParameters setup =
					new(CreateRegistry(), "M", matcher, "p1", "p2", "p3");

				bool result = setup.Matches("a", 1, 2.5);

				await That(result).IsTrue();
				await That(matcher.Received).HasCount(1);
				await That(matcher.Received[0]).IsEqualTo(new[]
				{
					("p1", "a"), ("p2", 1), ("p3", (object?)2.5),
				});
			}

			[Test]
			public async Task Matches_WithIParametersMatch_ShouldForwardAllParameterValues()
			{
				RecordingParametersMatch matcher = new(true);
				ReturnMethodSetup<int, string, int, double>.WithParameters setup =
					new(CreateRegistry(), "M", matcher, "p1", "p2", "p3");

				bool result = setup.Matches("a", 1, 2.5);

				await That(result).IsTrue();
				await That(matcher.Received).HasCount(1);
				await That(matcher.Received[0]).IsEqualTo(new object?[]
				{
					"a", 1, 2.5,
				});
			}

			[Test]
			public async Task Matches_WithUnknownIParameters_ShouldReturnTrue()
			{
				ReturnMethodSetup<int, string, int, double>.WithParameters setup =
					new(CreateRegistry(), "M", Match.AnyParameters(), "p1", "p2", "p3");

				bool result = setup.Matches("a", 1, 2.5);

				await That(result).IsTrue();
			}

			[Test]
			public async Task MatchesInteraction_WithNonMethodInvocationOfT1T2T3_ShouldReturnFalse()
			{
				ReturnMethodSetup<int, string, int, double>.WithParameters setup =
					new(CreateRegistry(), "M", Match.AnyParameters(), "p1", "p2", "p3");

				bool result = ((IVerifiableMethodSetup)setup).Matches(new MethodInvocation("M"));

				await That(result).IsFalse();
			}
		}

		public sealed class FourParameters
		{
			[Test]
			public async Task Matches_WithINamedParametersMatch_ShouldForwardAllNamedParameterValues()
			{
				RecordingNamedParametersMatch matcher = new(true);
				ReturnMethodSetup<int, string, int, double, bool>.WithParameters setup =
					new(CreateRegistry(), "M", matcher, "p1", "p2", "p3", "p4");

				bool result = setup.Matches("a", 1, 2.5, true);

				await That(result).IsTrue();
				await That(matcher.Received).HasCount(1);
				await That(matcher.Received[0]).IsEqualTo(new[]
				{
					("p1", "a"), ("p2", 1), ("p3", 2.5), ("p4", (object?)true),
				});
			}

			[Test]
			public async Task Matches_WithIParametersMatch_ShouldForwardAllParameterValues()
			{
				RecordingParametersMatch matcher = new(true);
				ReturnMethodSetup<int, string, int, double, bool>.WithParameters setup =
					new(CreateRegistry(), "M", matcher, "p1", "p2", "p3", "p4");

				bool result = setup.Matches("a", 1, 2.5, true);

				await That(result).IsTrue();
				await That(matcher.Received).HasCount(1);
				await That(matcher.Received[0]).IsEqualTo(new object?[]
				{
					"a", 1, 2.5, true,
				});
			}

			[Test]
			public async Task Matches_WithUnknownIParameters_ShouldReturnTrue()
			{
				ReturnMethodSetup<int, string, int, double, bool>.WithParameters setup =
					new(CreateRegistry(), "M", Match.AnyParameters(), "p1", "p2", "p3", "p4");

				bool result = setup.Matches("a", 1, 2.5, true);

				await That(result).IsTrue();
			}

			[Test]
			public async Task MatchesInteraction_WithNonMethodInvocationOfT1T2T3T4_ShouldReturnFalse()
			{
				ReturnMethodSetup<int, string, int, double, bool>.WithParameters setup =
					new(CreateRegistry(), "M", Match.AnyParameters(), "p1", "p2", "p3", "p4");

				bool result = ((IVerifiableMethodSetup)setup).Matches(new MethodInvocation("M"));

				await That(result).IsFalse();
			}
		}
	}

	public sealed class VoidMethodSetupTests
	{
		public sealed class OneParameter
		{
			[Test]
			public async Task Matches_WithINamedParametersMatch_ShouldForwardNamedParameterValue()
			{
				RecordingNamedParametersMatch matcher = new(true);
				VoidMethodSetup<string>.WithParameters setup =
					new(CreateRegistry(), "M", matcher, "p1");

				bool result = setup.Matches("hello");

				await That(result).IsTrue();
				await That(matcher.Received).HasCount(1);
				await That(matcher.Received[0]).IsEqualTo(new[]
				{
					("p1", (object?)"hello"),
				});
			}

			[Test]
			public async Task Matches_WithIParametersMatch_ShouldForwardSingleParameterValue()
			{
				RecordingParametersMatch matcher = new(true);
				VoidMethodSetup<string>.WithParameters setup =
					new(CreateRegistry(), "M", matcher, "p1");

				bool result = setup.Matches("hello");

				await That(result).IsTrue();
				await That(matcher.Received).HasCount(1);
				await That(matcher.Received[0]).IsEqualTo(new object?[]
				{
					"hello",
				});
			}

			[Test]
			public async Task Matches_WithUnknownIParameters_ShouldReturnTrue()
			{
				VoidMethodSetup<string>.WithParameters setup =
					new(CreateRegistry(), "M", Match.AnyParameters(), "p1");

				bool result = setup.Matches("anything");

				await That(result).IsTrue();
			}

			[Test]
			public async Task MatchesInteraction_WithNonMethodInvocationOfT1_ShouldReturnFalse()
			{
				VoidMethodSetup<string>.WithParameters setup =
					new(CreateRegistry(), "M", Match.AnyParameters(), "p1");

				bool result = ((IVerifiableMethodSetup)setup).Matches(new MethodInvocation("M"));

				await That(result).IsFalse();
			}
		}

		public sealed class TwoParameters
		{
			[Test]
			public async Task Matches_WithINamedParametersMatch_ShouldForwardBothNamedParameterValues()
			{
				RecordingNamedParametersMatch matcher = new(true);
				VoidMethodSetup<string, int>.WithParameters setup =
					new(CreateRegistry(), "M", matcher, "p1", "p2");

				bool result = setup.Matches("a", 1);

				await That(result).IsTrue();
				await That(matcher.Received).HasCount(1);
				await That(matcher.Received[0]).IsEqualTo(new[]
				{
					("p1", "a"), ("p2", (object?)1),
				});
			}

			[Test]
			public async Task Matches_WithIParametersMatch_ShouldForwardBothParameterValues()
			{
				RecordingParametersMatch matcher = new(true);
				VoidMethodSetup<string, int>.WithParameters setup =
					new(CreateRegistry(), "M", matcher, "p1", "p2");

				bool result = setup.Matches("a", 1);

				await That(result).IsTrue();
				await That(matcher.Received).HasCount(1);
				await That(matcher.Received[0]).IsEqualTo(new object?[]
				{
					"a", 1,
				});
			}

			[Test]
			public async Task Matches_WithUnknownIParameters_ShouldReturnTrue()
			{
				VoidMethodSetup<string, int>.WithParameters setup =
					new(CreateRegistry(), "M", Match.AnyParameters(), "p1", "p2");

				bool result = setup.Matches("a", 1);

				await That(result).IsTrue();
			}

			[Test]
			public async Task MatchesInteraction_WithNonMethodInvocationOfT1AndT2_ShouldReturnFalse()
			{
				VoidMethodSetup<string, int>.WithParameters setup =
					new(CreateRegistry(), "M", Match.AnyParameters(), "p1", "p2");

				bool result = ((IVerifiableMethodSetup)setup).Matches(new MethodInvocation("M"));

				await That(result).IsFalse();
			}
		}

		public sealed class ThreeParameters
		{
			[Test]
			public async Task Matches_WithINamedParametersMatch_ShouldForwardAllNamedParameterValues()
			{
				RecordingNamedParametersMatch matcher = new(true);
				VoidMethodSetup<string, int, double>.WithParameters setup =
					new(CreateRegistry(), "M", matcher, "p1", "p2", "p3");

				bool result = setup.Matches("a", 1, 2.5);

				await That(result).IsTrue();
				await That(matcher.Received).HasCount(1);
				await That(matcher.Received[0]).IsEqualTo(new[]
				{
					("p1", "a"), ("p2", 1), ("p3", (object?)2.5),
				});
			}

			[Test]
			public async Task Matches_WithIParametersMatch_ShouldForwardAllParameterValues()
			{
				RecordingParametersMatch matcher = new(true);
				VoidMethodSetup<string, int, double>.WithParameters setup =
					new(CreateRegistry(), "M", matcher, "p1", "p2", "p3");

				bool result = setup.Matches("a", 1, 2.5);

				await That(result).IsTrue();
				await That(matcher.Received).HasCount(1);
				await That(matcher.Received[0]).IsEqualTo(new object?[]
				{
					"a", 1, 2.5,
				});
			}

			[Test]
			public async Task Matches_WithUnknownIParameters_ShouldReturnTrue()
			{
				VoidMethodSetup<string, int, double>.WithParameters setup =
					new(CreateRegistry(), "M", Match.AnyParameters(), "p1", "p2", "p3");

				bool result = setup.Matches("a", 1, 2.5);

				await That(result).IsTrue();
			}

			[Test]
			public async Task MatchesInteraction_WithNonMethodInvocationOfT1T2T3_ShouldReturnFalse()
			{
				VoidMethodSetup<string, int, double>.WithParameters setup =
					new(CreateRegistry(), "M", Match.AnyParameters(), "p1", "p2", "p3");

				bool result = ((IVerifiableMethodSetup)setup).Matches(new MethodInvocation("M"));

				await That(result).IsFalse();
			}
		}

		public sealed class FourParameters
		{
			[Test]
			public async Task Matches_WithINamedParametersMatch_ShouldForwardAllNamedParameterValues()
			{
				RecordingNamedParametersMatch matcher = new(true);
				VoidMethodSetup<string, int, double, bool>.WithParameters setup =
					new(CreateRegistry(), "M", matcher, "p1", "p2", "p3", "p4");

				bool result = setup.Matches("a", 1, 2.5, true);

				await That(result).IsTrue();
				await That(matcher.Received).HasCount(1);
				await That(matcher.Received[0]).IsEqualTo(new[]
				{
					("p1", "a"), ("p2", 1), ("p3", 2.5), ("p4", (object?)true),
				});
			}

			[Test]
			public async Task Matches_WithIParametersMatch_ShouldForwardAllParameterValues()
			{
				RecordingParametersMatch matcher = new(true);
				VoidMethodSetup<string, int, double, bool>.WithParameters setup =
					new(CreateRegistry(), "M", matcher, "p1", "p2", "p3", "p4");

				bool result = setup.Matches("a", 1, 2.5, true);

				await That(result).IsTrue();
				await That(matcher.Received).HasCount(1);
				await That(matcher.Received[0]).IsEqualTo(new object?[]
				{
					"a", 1, 2.5, true,
				});
			}

			[Test]
			public async Task Matches_WithUnknownIParameters_ShouldReturnTrue()
			{
				VoidMethodSetup<string, int, double, bool>.WithParameters setup =
					new(CreateRegistry(), "M", Match.AnyParameters(), "p1", "p2", "p3", "p4");

				bool result = setup.Matches("a", 1, 2.5, true);

				await That(result).IsTrue();
			}

			[Test]
			public async Task MatchesInteraction_WithNonMethodInvocationOfT1T2T3T4_ShouldReturnFalse()
			{
				VoidMethodSetup<string, int, double, bool>.WithParameters setup =
					new(CreateRegistry(), "M", Match.AnyParameters(), "p1", "p2", "p3", "p4");

				bool result = ((IVerifiableMethodSetup)setup).Matches(new MethodInvocation("M"));

				await That(result).IsFalse();
			}
		}
	}
}
