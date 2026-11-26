using System.Threading.Tasks;
using Verifier = Mockolate.Analyzers.Tests.Verifiers.CSharpSuppressorVerifier<Mockolate.Analyzers.AsDefaultSuppressor>;

namespace Mockolate.Analyzers.Tests;

public class AsDefaultSuppressorTests
{
	private const string Attributes = """
	                                  [System.AttributeUsage(System.AttributeTargets.Method)]
	                                  public sealed class DataAttribute : System.Attribute, Mockolate.Internals.IMockDataSource;

	                                  [System.AttributeUsage(System.AttributeTargets.Method)]
	                                  public sealed class OtherAttribute : System.Attribute;
	                                  """;

	private const string Markers = """
	                               namespace Mockolate.Integrations
	                               {
	                                   public abstract class AsDefaultParameterAttribute : System.Attribute;
	                                   public abstract class TransientParameterAttribute : System.Attribute;
	                                   public sealed class AsDefaultAttribute(params System.Type[] types) : AsDefaultParameterAttribute;
	                                   public sealed class AsDefaultAttribute<T1> : AsDefaultParameterAttribute;
	                                   public sealed class TransientAttribute : TransientParameterAttribute;
	                               }
	                               """;

	[Test]
	[Arguments("IDE0060")]
	[Arguments("xUnit1026")]
	public async Task WhenAsDefaultParameterOfMockDataSource_ShouldSuppressUnusedParameter(string diagnosticId) => await Verifier
		.VerifySuppressorAsync(diagnosticId,
			$$"""
			  namespace MyCode
			  {
			      using Mockolate.Integrations;

			      public interface IDep { }

			      public class Tests
			      {
			          [Data]
			          public void Test([AsDefault] int {|#0:given|}, [AsDefault] IDep {|#1:dep|},
			              [AsDefault(typeof(object))] IDep {|#2:typed|}, [AsDefault<object>] IDep {|#3:generic|}) { }
			      }

			      {{Attributes}}
			  }

			  {{Markers}}
			  """,
			Verifier.UnusedParameter(diagnosticId, 0, true),
			Verifier.UnusedParameter(diagnosticId, 1, true),
			Verifier.UnusedParameter(diagnosticId, 2, true),
			Verifier.UnusedParameter(diagnosticId, 3, true)
		);

	[Test]
	[Arguments("IDE0060")]
	[Arguments("xUnit1026")]
	public async Task WhenParameterIsNotAsDefault_ShouldNotSuppressUnusedParameter(string diagnosticId) => await Verifier
		.VerifySuppressorAsync(diagnosticId,
			$$"""
			  namespace MyCode
			  {
			      using Mockolate.Integrations;

			      public interface IDep { }

			      public class Tests
			      {
			          [Data]
			          public void Test(int {|#0:given|}, IDep {|#1:dep|}, [Transient] IDep {|#2:transient|}) { }
			      }

			      {{Attributes}}
			  }

			  {{Markers}}
			  """,
			Verifier.UnusedParameter(diagnosticId, 0, false),
			Verifier.UnusedParameter(diagnosticId, 1, false),
			Verifier.UnusedParameter(diagnosticId, 2, false)
		);

	[Test]
	[Arguments("IDE0060")]
	[Arguments("xUnit1026")]
	public async Task WhenMethodHasNoMockDataSource_ShouldNotSuppressUnusedParameter(string diagnosticId) => await Verifier
		.VerifySuppressorAsync(diagnosticId,
			$$"""
			  namespace MyCode
			  {
			      using Mockolate.Integrations;

			      public interface IDep { }

			      public class Tests
			      {
			          [Other]
			          public void Test([AsDefault] IDep {|#0:dep|}) { }
			      }

			      {{Attributes}}
			  }

			  {{Markers}}
			  """,
			Verifier.UnusedParameter(diagnosticId, 0, false)
		);

	[Test]
	[Arguments("IDE0060")]
	[Arguments("xUnit1026")]
	public async Task WhenDiagnosticIsNotAtTheParameter_ShouldNotSuppressUnusedParameter(string diagnosticId) => await Verifier
		.VerifySuppressorAsync(diagnosticId,
			$$"""
			  namespace MyCode
			  {
			      using Mockolate.Integrations;

			      public interface IDep { }

			      public class Tests
			      {
			          [Data]
			          public void {|#0:Test|}([AsDefault] IDep atMethod, [AsDefault] IDep noLocation) { }
			      }

			      {{Attributes}}
			  }

			  {{Markers}}
			  """,
			Verifier.UnusedParameter(diagnosticId, 0, false),
			Verifier.UnusedParameter(diagnosticId, false)
		);

	[Test]
	[Arguments("IDE0060")]
	[Arguments("xUnit1026")]
	public async Task WhenParameterIsNotOfAMethod_ShouldNotSuppressUnusedParameter(string diagnosticId) => await Verifier
		.VerifySuppressorAsync(diagnosticId,
			$$"""
			  namespace MyCode
			  {
			      using Mockolate.Integrations;

			      public interface IDep { }

			      public class Tests
			      {
			          public int this[[AsDefault] IDep {|#0:dep|}] => 0;
			      }

			      {{Attributes}}
			  }

			  {{Markers}}
			  """,
			Verifier.UnusedParameter(diagnosticId, 0, false)
		);
}
