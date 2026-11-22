using Mockolate.Analyzers;

namespace Mockolate.SourceGenerators.Tests;

public sealed partial class CreateWithMocksTests
{
	/// <summary>
	///     Runs the <see cref="CreateWithMocksAnalyzer" /> on the generated code, as in a build, where the analyzer sees
	///     the generated <c>CreateWithMocks</c> methods and containers.
	/// </summary>
	public sealed class AnalyzerTests
	{
		private const string Types = """
		                             namespace MyCode
		                             {
		                                 public interface IDep
		                                 {
		                                     int Get();
		                                 }

		                                 public class TimeSource
		                                 {
		                                 }

		                                 public class Svc(IDep dep, TimeSource clock)
		                                 {
		                                     public IDep Dep { get; } = dep;
		                                     public TimeSource Clock { get; } = clock;
		                                 }

		                                 public class Other(IDep dep)
		                                 {
		                                     public IDep Dep { get; } = dep;
		                                 }
		                             }

		                             """;

		[Test]
		public async Task MissingClassDependency_ShouldReportMockolate0009()
		{
			string[] diagnostics = await Generator.RunWithAnalyzer(Types + """
			                                                               namespace MyCode
			                                                               {
			                                                                   using Mockolate;

			                                                                   public class Tests
			                                                                   {
			                                                                       public void Run()
			                                                                       {
			                                                                           _ = Svc.CreateWithMocks(out _);
			                                                                       }
			                                                                   }
			                                                               }
			                                                               """, new CreateWithMocksAnalyzer());

			await That(diagnostics).Contains(
				"Mockolate0009: The parameter 'clock' of type 'MyCode.TimeSource' needs an argument, because it is not mocked: only interfaces are mocked");
			await That(diagnostics).None().Satisfy(diagnostic => diagnostic.StartsWith("Mockolate0008"));
		}

		[Test]
		public async Task MissingDependencyNextToOtherClassesUnderTest_ShouldReportMockolate0009()
		{
			string[] diagnostics = await Generator.RunWithAnalyzer(Types + """
			                                                               namespace MyCode
			                                                               {
			                                                                   using Mockolate;

			                                                                   public class Tests
			                                                                   {
			                                                                       public void Run()
			                                                                       {
			                                                                           _ = Other.CreateWithMocks(out _);
			                                                                           _ = Svc.CreateWithMocks(out _);
			                                                                       }
			                                                                   }
			                                                               }
			                                                               """, new CreateWithMocksAnalyzer());

			await That(diagnostics).Contains(
				"Mockolate0009: The parameter 'clock' of type 'MyCode.TimeSource' needs an argument, because it is not mocked: only interfaces are mocked");
			await That(diagnostics).None().Satisfy(diagnostic => diagnostic.StartsWith("Mockolate0008"));
		}

		[Test]
		public async Task WithoutMockolateUsing_ShouldReportMockolate0009()
		{
			string[] diagnostics = await Generator.RunWithAnalyzer(Types + """
			                                                               namespace MyCode
			                                                               {
			                                                                   public class Tests
			                                                                   {
			                                                                       public void Run()
			                                                                       {
			                                                                           _ = Svc.CreateWithMocks(out _);
			                                                                       }
			                                                                   }
			                                                               }
			                                                               """, new CreateWithMocksAnalyzer());

			await That(diagnostics).Contains(
				"Mockolate0009: The parameter 'clock' of type 'MyCode.TimeSource' needs an argument, because it is not mocked: only interfaces are mocked");
			await That(diagnostics).None().Satisfy(diagnostic => diagnostic.StartsWith("Mockolate0008"));
		}

		[Test]
		public async Task AbstractClass_ShouldReportMockolate0008()
		{
			string[] diagnostics = await Generator.RunWithAnalyzer(Types + """
			                                                               namespace MyCode
			                                                               {
			                                                                   using Mockolate;

			                                                                   public abstract class Machine(IDep dep);

			                                                                   public class Tests
			                                                                   {
			                                                                       public void Run()
			                                                                       {
			                                                                           _ = Other.CreateWithMocks(out _);
			                                                                           _ = Machine.CreateWithMocks(out _);
			                                                                       }
			                                                                   }
			                                                               }
			                                                               """, new CreateWithMocksAnalyzer());

			await That(diagnostics).Contains("Mockolate0008: 'MyCode.Machine' cannot be created with mocks: it is abstract");
		}

		[Test]
		public async Task ValidCalls_ShouldReportNothing()
		{
			string[] diagnostics = await Generator.RunWithAnalyzer(Types + """
			                                                               namespace MyCode
			                                                               {
			                                                                   using Mockolate;

			                                                                   public class Tests
			                                                                   {
			                                                                       public void Run()
			                                                                       {
			                                                                           _ = Other.CreateWithMocks(out OtherMocks otherMocks);
			                                                                           _ = Svc.CreateWithMocks(out SvcMocks mocks, new TimeSource());
			                                                                           _ = Svc.CreateWithMocks(out _, clock: new TimeSource(), dep: otherMocks.Dep);
			                                                                       }
			                                                                   }
			                                                               }
			                                                               """, new CreateWithMocksAnalyzer());

			await That(diagnostics).IsEmpty();
		}

		[Test]
		public async Task ReceiverHiddenByMemberOfTheSameName_ShouldCreateTheClass()
		{
			string[] diagnostics = await Generator.RunWithAnalyzer(Types + """
			                                                               namespace MyCode
			                                                               {
			                                                                   using Mockolate;

			                                                                   public class Tests
			                                                                   {
			                                                                       public Other Other { get; set; } = null!;

			                                                                       public void Run()
			                                                                       {
			                                                                           Other = Other.CreateWithMocks(out _);
			                                                                       }
			                                                                   }
			                                                               }
			                                                               """, new CreateWithMocksAnalyzer());

			await That(diagnostics).IsEmpty();
		}
	}
}
