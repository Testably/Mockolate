using Microsoft.CodeAnalysis.CSharp;

namespace Mockolate.SourceGenerators.Tests;

// Only compiled for net10.0, which references MinimumRoslynVersion, a compiler without C# 15.
public sealed class MinimumRoslynTests
{
	[Test]
	[Arguments(LanguageVersion.Latest)]
	[Arguments(LanguageVersion.Preview)]
	public async Task WithoutCSharp15Compiler_ShouldNotEmitParameterArg(LanguageVersion languageVersion)
	{
		GeneratorResult result = Generator.Run("""
		                                       using Mockolate;

		                                       public interface IMyInterface
		                                       {
		                                           bool MyFunc(int value);
		                                       }

		                                       public class Program
		                                       {
		                                           public static void Main(string[] args)
		                                           {
		                                               _ = IMyInterface.CreateMock();
		                                           }
		                                       }
		                                       """, languageVersion, null);

		await That(result.Sources.Keys).DoesNotContain("ParameterArg.g.cs");
		await That(result.Diagnostics).IsEmpty();
	}
}
