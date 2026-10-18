namespace Mockolate.Tests.MockMethods;

public sealed partial class SetupMethodTests
{
	/// <summary>
	///     Behavioral coverage for the fused <c>WithLiteralValues</c> setup path and the literal-value
	///     <c>VerifyMethod</c> overloads (arities 1..4, return + void). The bare-value setup overload
	///     used to construct an <see cref="It.IsValue{T}" /> matcher per call; these tests pin the
	///     equivalence between the literal-value path and the explicit-matcher path so the
	///     allocation-saving rewrite cannot regress matching semantics.
	/// </summary>
	public sealed class LiteralValuesTests
	{
		[Test]
		public async Task LiteralValues_Arity1_Return_MatchesValue()
		{
			ILiteralFusedService sut = ILiteralFusedService.CreateMock();
			sut.Mock.Setup.Get1(1).Returns("one");

			string match = sut.Get1(1);
			string miss = sut.Get1(2);

			await That(match).IsEqualTo("one");
			await That(miss).IsEqualTo(string.Empty);
		}

		[Test]
		public async Task LiteralValues_Arity1_Void_MatchesAndCounts()
		{
			ILiteralFusedService sut = ILiteralFusedService.CreateMock();
			int hits = 0;
			sut.Mock.Setup.Touch1(1).Do(() => hits++);

			sut.Touch1(1);
			sut.Touch1(2);

			await That(hits).IsEqualTo(1);
			await That(sut.Mock.Verify.Touch1(1)).Once();
			await That(sut.Mock.Verify.Touch1(2)).Once();
		}

		[Test]
		public async Task LiteralValues_Arity1_AnyParameters_MatchesEverything()
		{
			ILiteralFusedService sut = ILiteralFusedService.CreateMock();
			sut.Mock.Setup.Get1(1).AnyParameters().Returns("any");

			await That(sut.Get1(1)).IsEqualTo("any");
			await That(sut.Get1(99)).IsEqualTo("any");
		}

		[Test]
		public async Task LiteralValues_Arity1_EquivalentToItIs()
		{
			ILiteralFusedService literal = ILiteralFusedService.CreateMock();
			ILiteralFusedService matcher = ILiteralFusedService.CreateMock();
			literal.Mock.Setup.Get1(7).Returns("seven");
			matcher.Mock.Setup.Get1(It.Is(7)).Returns("seven");

			await That(literal.Get1(7)).IsEqualTo(matcher.Get1(7));
			await That(literal.Get1(8)).IsEqualTo(matcher.Get1(8));
		}

		[Test]
		public async Task LiteralValues_Arity2_Return_MatchesAllParameters()
		{
			ILiteralFusedService sut = ILiteralFusedService.CreateMock();
			sut.Mock.Setup.Get2(1, "a").Returns(11);

			await That(sut.Get2(1, "a")).IsEqualTo(11);
			await That(sut.Get2(1, "b")).IsEqualTo(0);
			await That(sut.Get2(2, "a")).IsEqualTo(0);
			await That(sut.Mock.Verify.Get2(1, "a")).Once();
			await That(sut.Mock.Verify.Get2(2, "a")).Once();
		}

		[Test]
		public async Task LiteralValues_Arity2_Void_VerifiesByLiteralValues()
		{
			ILiteralFusedService sut = ILiteralFusedService.CreateMock();
			sut.Touch2(1, true);
			sut.Touch2(1, false);
			sut.Touch2(2, true);

			await That(sut.Mock.Verify.Touch2(1, true)).Once();
			await That(sut.Mock.Verify.Touch2(1, false)).Once();
			await That(sut.Mock.Verify.Touch2(3, true)).Never();
		}

		[Test]
		public async Task LiteralValues_Arity3_Return_MatchesAllParameters()
		{
			ILiteralFusedService sut = ILiteralFusedService.CreateMock();
			sut.Mock.Setup.Get3(1, "a", true).Returns(33);

			await That(sut.Get3(1, "a", true)).IsEqualTo(33);
			await That(sut.Get3(1, "a", false)).IsEqualTo(0);
			await That(sut.Mock.Verify.Get3(1, "a", true)).Once();
			await That(sut.Mock.Verify.Get3(1, "a", false)).Once();
		}

		[Test]
		public async Task LiteralValues_Arity3_Void_AnyParameters()
		{
			ILiteralFusedService sut = ILiteralFusedService.CreateMock();
			int hits = 0;
			sut.Mock.Setup.Touch3(0, "x", false).AnyParameters().Do(() => hits++);

			sut.Touch3(0, "x", false);
			sut.Touch3(99, "y", true);

			await That(hits).IsEqualTo(2);
		}

		[Test]
		public async Task LiteralValues_Arity4_Return_MatchesAllParameters()
		{
			ILiteralFusedService sut = ILiteralFusedService.CreateMock();
			sut.Mock.Setup.Get4(1, "a", true, 0.5).Returns(44);

			await That(sut.Get4(1, "a", true, 0.5)).IsEqualTo(44);
			await That(sut.Get4(1, "a", true, 0.6)).IsEqualTo(0);
			await That(sut.Mock.Verify.Get4(1, "a", true, 0.5)).Once();
			await That(sut.Mock.Verify.Get4(1, "a", true, 0.6)).Once();
		}

		[Test]
		public async Task LiteralValues_Arity4_Void_EquivalentToMatcherSetup()
		{
			ILiteralFusedService literal = ILiteralFusedService.CreateMock();
			ILiteralFusedService matcher = ILiteralFusedService.CreateMock();
			int literalHits = 0;
			int matcherHits = 0;
			literal.Mock.Setup.Touch4(1, "a", true, 1.5).Do(() => literalHits++);
			matcher.Mock.Setup.Touch4(It.Is(1), It.Is("a"), It.Is(true), It.Is(1.5)).Do(() => matcherHits++);

			literal.Touch4(1, "a", true, 1.5);
			matcher.Touch4(1, "a", true, 1.5);
			literal.Touch4(2, "a", true, 1.5);
			matcher.Touch4(2, "a", true, 1.5);

			await That(literalHits).IsEqualTo(matcherHits);
		}

		[Test]
		public async Task LiteralValues_ToString_UsesInvariantCulture()
		{
			ILiteralFusedService sut = ILiteralFusedService.CreateMock();
			sut.Mock.Setup.Get4(1, "a", true, 0.5).Returns(44);
			Mockolate.MockRegistry registry = ((Mockolate.IMock)sut).MockRegistry;

			System.Collections.Generic.IReadOnlyCollection<Mockolate.Setup.ISetup> unused =
				registry.GetUnusedSetups(new Mockolate.Interactions.FastMockInteractions(0));
			Mockolate.Setup.ISetup setup = await That(unused).HasSingle();

			await That(setup.ToString()!).Contains("0.5")
				.Because(
					"FormatLiteralValue must use InvariantCulture; on a German-locale host the host default would render 0.5 as \"0,5\"");
		}
	}

	public interface ILiteralFusedService
	{
		string Get1(int p1);
		void Touch1(int p1);

		int Get2(int p1, string p2);
		void Touch2(int p1, bool p2);

		int Get3(int p1, string p2, bool p3);
		void Touch3(int p1, string p2, bool p3);

		int Get4(int p1, string p2, bool p3, double p4);
		void Touch4(int p1, string p2, bool p3, double p4);
	}
}
