using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Verifier = Mockolate.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<Mockolate.Analyzers.LoggingAnalyzer>;

namespace Mockolate.Analyzers.Tests;

public class LoggingAnalyzerTests
{
	private const string Prefix = """
	                              #nullable enable
	                              using System;
	                              using Microsoft.Extensions.Logging;
	                              using Mockolate;
	                              using Mockolate.Setup;
	                              using Mockolate.Verify;

	                              namespace Microsoft.Extensions.Logging
	                              {
	                              	public interface ILogger
	                              	{
	                              		void Log<TState>(int logLevel, TState state);
	                              	}

	                              	public interface ILoggerFactory
	                              	{
	                              		ILogger CreateLogger(string categoryName);
	                              	}
	                              }

	                              namespace Mockolate
	                              {
	                              	internal static class MockolateLogging
	                              	{
	                              		public static object? LogError(this IMockSetup<ILogger> setup) => null;
	                              		public static object? LogError(this IMockVerify<ILogger> verify) => null;

	                              		extension<TVerify>(TVerify verify) where TVerify : class, IMockVerify<ILogger>
	                              		{
	                              			public object? LogWarning() => null;
	                              		}
	                              	}

	                              	internal static class MockolateLoggerFactoryLogging
	                              	{
	                              		extension<TVerify>(TVerify verify) where TVerify : class, IMockVerify<ILoggerFactory>
	                              		{
	                              			public object? LogCritical() => null;
	                              		}
	                              	}
	                              }

	                              namespace MyCode
	                              {
	                              	public interface ISetup<out T> : IMockSetup<T> { }
	                              	public interface IVerify<out T> : IMockVerify<T> { }
	                              }

	                              """;

	[Test]
	public async Task WhenClassImplementsLogWithNonVirtualMethod_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			Prefix + """
			         namespace MyCode
			         {
			         	public class MyLogger : ILogger
			         	{
			         		public void Log<TState>(int logLevel, TState state) { }
			         	}

			         	public class MyClass
			         	{
			         		public void MyTest(ISetup<MyLogger> setup, IVerify<MyLogger> verify)
			         		{
			         			_ = setup.{|#0:LogError|}();
			         			_ = verify.{|#1:LogError|}();
			         		}
			         	}
			         }
			         """,
			new DiagnosticResult("Mockolate0005", DiagnosticSeverity.Error)
				.WithLocation(0).WithArguments("MyCode.MyLogger"),
			new DiagnosticResult("Mockolate0005", DiagnosticSeverity.Error)
				.WithLocation(1).WithArguments("MyCode.MyLogger"));

	[Test]
	public async Task WhenExtensionBlockMemberIsUsedOnClassWithNonVirtualLog_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			Prefix + """
			         namespace MyCode
			         {
			         	public class MyLogger : ILogger
			         	{
			         		public void Log<TState>(int logLevel, TState state) { }
			         	}

			         	public class MyClass
			         	{
			         		public void MyTest(IVerify<MyLogger> verify, IVerify<ILogger> interfaceVerify)
			         		{
			         			_ = verify.{|#0:LogWarning|}();
			         			_ = interfaceVerify.LogWarning();
			         		}
			         	}
			         }
			         """,
			new DiagnosticResult("Mockolate0005", DiagnosticSeverity.Error)
				.WithLocation(0).WithArguments("MyCode.MyLogger"));

	[Test]
	public async Task WhenLoggerFactoryIsAClass_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			Prefix + """
			         namespace MyCode
			         {
			         	public class MyLoggerFactory : ILoggerFactory
			         	{
			         		public virtual ILogger CreateLogger(string categoryName) => null!;
			         	}

			         	public class MyClass
			         	{
			         		public void MyTest(IVerify<MyLoggerFactory> verify, IVerify<ILoggerFactory> interfaceVerify)
			         		{
			         			_ = verify.{|#0:LogCritical|}();
			         			_ = interfaceVerify.LogCritical();
			         		}
			         	}
			         }
			         """,
			new DiagnosticResult("Mockolate0005", DiagnosticSeverity.Error)
				.WithLocation(0).WithArguments("MyCode.MyLoggerFactory"));

	[Test]
	public async Task WhenClassImplementsLogExplicitlyAndForwardsToVirtualMethod_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			Prefix + """
			         namespace MyCode
			         {
			         	public class MyLogger : ILogger
			         	{
			         		void ILogger.Log<TState>(int logLevel, TState state) => Log(logLevel, state);
			         		public virtual void Log<TState>(int logLevel, TState state) { }
			         	}

			         	public class MyClass
			         	{
			         		public void MyTest(ISetup<MyLogger> setup)
			         		{
			         			_ = setup.{|#0:LogError|}();
			         		}
			         	}
			         }
			         """,
			new DiagnosticResult("Mockolate0005", DiagnosticSeverity.Error)
				.WithLocation(0).WithArguments("MyCode.MyLogger"));

	[Test]
	public async Task WhenClassImplementsLogExplicitly_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			Prefix + """
			         namespace MyCode
			         {
			         	public class MyLogger : ILogger
			         	{
			         		void ILogger.Log<TState>(int logLevel, TState state) { }
			         	}

			         	public class MyClass
			         	{
			         		public void MyTest(ISetup<MyLogger> setup)
			         		{
			         			_ = setup.{|#0:LogError|}();
			         		}
			         	}
			         }
			         """,
			new DiagnosticResult("Mockolate0005", DiagnosticSeverity.Error)
				.WithLocation(0).WithArguments("MyCode.MyLogger"));

	[Test]
	public async Task WhenClassSealsLog_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			Prefix + """
			         namespace MyCode
			         {
			         	public abstract class LoggerBase : ILogger
			         	{
			         		public abstract void Log<TState>(int logLevel, TState state);
			         	}

			         	public class MyLogger : LoggerBase
			         	{
			         		public sealed override void Log<TState>(int logLevel, TState state) { }
			         	}

			         	public class MyClass
			         	{
			         		public void MyTest(ISetup<MyLogger> setup)
			         		{
			         			_ = setup.{|#0:LogError|}();
			         		}
			         	}
			         }
			         """,
			new DiagnosticResult("Mockolate0005", DiagnosticSeverity.Error)
				.WithLocation(0).WithArguments("MyCode.MyLogger"));

	[Test]
	public async Task WhenClassImplementsLogWithOverridableMethod_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			Prefix + """
			         namespace MyCode
			         {
			         	public abstract class AbstractLogger : ILogger
			         	{
			         		public abstract void Log<TState>(int logLevel, TState state);
			         	}

			         	public class VirtualLogger : ILogger
			         	{
			         		public virtual void Log<TState>(int logLevel, TState state) { }
			         	}

			         	public class OverridingLogger : AbstractLogger
			         	{
			         		public override void Log<TState>(int logLevel, TState state) { }
			         	}

			         	public class NonLoggerBase
			         	{
			         		public virtual void Log<TState>(int logLevel, TState state) { }
			         	}

			         	public class LoggerWithLogFromNonLoggerBase : NonLoggerBase, ILogger
			         	{
			         	}

			         	public class MyClass
			         	{
			         		public void MyTest(ISetup<AbstractLogger> abstractSetup, ISetup<VirtualLogger> virtualSetup,
			         			IVerify<OverridingLogger> overridingVerify, ISetup<ILogger> interfaceSetup,
			         			ISetup<LoggerWithLogFromNonLoggerBase> inheritedSetup)
			         		{
			         			_ = inheritedSetup.LogError();
			         			_ = abstractSetup.LogError();
			         			_ = virtualSetup.LogError();
			         			_ = overridingVerify.LogError();
			         			_ = interfaceSetup.LogError();
			         		}
			         	}
			         }
			         """);
}
