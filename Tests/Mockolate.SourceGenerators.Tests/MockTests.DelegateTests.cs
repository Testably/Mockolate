namespace Mockolate.SourceGenerators.Tests;

public sealed partial class MockTests
{
	public sealed class DelegateTests
	{
		[Fact]
		public async Task CustomDelegates_ShouldOnlyCreateTwoExtensionsForSetupAndVerify()
		{
			GeneratorResult result = Generator
				.Run("""
				     using System;
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = DoSomething.CreateMock();
				         }

				         public delegate int DoSomething(int x, int y, out bool success);
				     }
				     """);

			await That(result.Sources).ContainsKey("Mock.Program_DoSomething.g.cs");
			await That(result.Sources["Mock.Program_DoSomething.g.cs"])
				.Contains("""
				          		global::Mockolate.Setup.IReturnMethodSetupWithCallback<int, int, int, bool> global::Mockolate.Mock.IMockSetupForProgram_DoSomething.Setup(global::Mockolate.Parameters.IParameter<int>? x, global::Mockolate.Parameters.IParameter<int>? y, global::Mockolate.Parameters.IOutParameter<bool> success)
				          		{
				          			var methodSetup = new global::Mockolate.Setup.ReturnMethodSetup<int, int, int, bool>.WithParameterCollection(MockRegistry, "global::MyCode.Program.DoSomething.Invoke", CovariantParameterAdapter<int>.Wrap(x ?? global::Mockolate.It.IsNull<int>("null")), CovariantParameterAdapter<int>.Wrap(y ?? global::Mockolate.It.IsNull<int>("null")), (global::Mockolate.Parameters.IParameterMatch<bool>)(success));
				          			this.MockRegistry.SetupMethod(global::Mockolate.Mock.Program_DoSomething.MemberId_Invoke, methodSetup);
				          			return methodSetup;
				          		}
				          """).IgnoringNewlineStyle().And
				.Contains("""
				          		global::Mockolate.Setup.IReturnMethodSetupWithCallback<int, int, int, bool> global::Mockolate.Mock.IMockSetupForProgram_DoSomething.Setup(global::Mockolate.Parameters.IParameters parameters)
				          		{
				          			var methodSetup = new global::Mockolate.Setup.ReturnMethodSetup<int, int, int, bool>.WithParameters(MockRegistry, "global::MyCode.Program.DoSomething.Invoke", parameters, "x", "y", "success");
				          			this.MockRegistry.SetupMethod(global::Mockolate.Mock.Program_DoSomething.MemberId_Invoke, methodSetup);
				          			return methodSetup;
				          		}
				          """).IgnoringNewlineStyle().And
				.Contains("""
				          		global::Mockolate.Verify.VerificationResult<IMockVerifyForProgram_DoSomething> IMockVerifyForProgram_DoSomething.Verify(global::Mockolate.Parameters.IParameter<int>? x, global::Mockolate.Parameters.IParameter<int>? y, global::Mockolate.Parameters.IVerifyOutParameter<bool> success)
				          """).IgnoringNewlineStyle().And
				.Contains("""
				          		global::Mockolate.Verify.VerificationResult<IMockVerifyForProgram_DoSomething> IMockVerifyForProgram_DoSomething.Verify(global::Mockolate.Parameters.IParameters parameters)
				          """).IgnoringNewlineStyle();
		}

		[Fact]
		public async Task CustomDelegates_ShouldSupportSpanAndReadOnlySpanReturnTypes()
		{
			GeneratorResult result = Generator
				.Run("""
				     using System;
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = DoSomething1.CreateMock();
				     		_ = DoSomething2.CreateMock();
				         }

				         public delegate Span<char> DoSomething1(int x);
				         public delegate ReadOnlySpan<char> DoSomething2(int x);
				     }
				     """);

			await That(result.Sources).ContainsKey("Mock.Program_DoSomething1.g.cs");
			await That(result.Sources["Mock.Program_DoSomething1.g.cs"])
				.Contains("""
				          		public global::MyCode.Program.DoSomething1 Object => new(Invoke);
				          		private global::System.Span<char> Invoke(int x)
				          		{
				          """)
				.IgnoringNewlineStyle().And
				.Contains(
					"foreach (global::Mockolate.Setup.ReturnMethodSetup<global::Mockolate.Setup.SpanWrapper<char>, int> s_methodSetup in this.MockRegistry.GetMethodSetups<global::Mockolate.Setup.ReturnMethodSetup<global::Mockolate.Setup.SpanWrapper<char>, int>>(\"global::MyCode.Program.DoSomething1.Invoke\"))")
				.IgnoringNewlineStyle().And
				.Contains("""
				          			if (MockRegistry.Behavior.SkipInteractionRecording == false)
				          			{
				          				MockRegistry.RegisterInteraction(new global::Mockolate.Interactions.MethodInvocation<int>("global::MyCode.Program.DoSomething1.Invoke", x));
				          			}
				          			if (methodSetup is null && this.MockRegistry.Behavior.ThrowWhenNotSetup)
				          			{
				          				throw new global::Mockolate.Exceptions.MockNotSetupException("The method 'global::MyCode.Program.DoSomething1.Invoke(int)' was invoked without prior setup.");
				          			}
				          			methodSetup?.TriggerCallbacks(x);
				          			return methodSetup?.TryGetReturnValue(x, out var returnValue) == true ? returnValue : this.MockRegistry.Behavior.DefaultValue.Generate(default(global::Mockolate.Setup.SpanWrapper<char>)!);
				          		}
				          """).IgnoringNewlineStyle();
			await That(result.Sources).ContainsKey("Mock.Program_DoSomething2.g.cs");
			await That(result.Sources["Mock.Program_DoSomething2.g.cs"])
				.Contains("""
				          		public global::MyCode.Program.DoSomething2 Object => new(Invoke);
				          		private global::System.ReadOnlySpan<char> Invoke(int x)
				          		{
				          """)
				.IgnoringNewlineStyle().And
				.Contains(
					"foreach (global::Mockolate.Setup.ReturnMethodSetup<global::Mockolate.Setup.ReadOnlySpanWrapper<char>, int> s_methodSetup in this.MockRegistry.GetMethodSetups<global::Mockolate.Setup.ReturnMethodSetup<global::Mockolate.Setup.ReadOnlySpanWrapper<char>, int>>(\"global::MyCode.Program.DoSomething2.Invoke\"))")
				.IgnoringNewlineStyle().And
				.Contains("""
				          			if (MockRegistry.Behavior.SkipInteractionRecording == false)
				          			{
				          				MockRegistry.RegisterInteraction(new global::Mockolate.Interactions.MethodInvocation<int>("global::MyCode.Program.DoSomething2.Invoke", x));
				          			}
				          			if (methodSetup is null && this.MockRegistry.Behavior.ThrowWhenNotSetup)
				          			{
				          				throw new global::Mockolate.Exceptions.MockNotSetupException("The method 'global::MyCode.Program.DoSomething2.Invoke(int)' was invoked without prior setup.");
				          			}
				          			methodSetup?.TriggerCallbacks(x);
				          			return methodSetup?.TryGetReturnValue(x, out var returnValue) == true ? returnValue : this.MockRegistry.Behavior.DefaultValue.Generate(default(global::Mockolate.Setup.ReadOnlySpanWrapper<char>)!);
				          		}
				          """).IgnoringNewlineStyle();
		}

		[Fact]
		public async Task CustomDelegates_WithSpanParameter_ShouldUseWrapperInsteadOfDegrading()
		{
			GeneratorResult result = Generator
				.Run("""
				     using System;
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = DoSomething1.CreateMock();
				         }

				         public delegate int DoSomething1(Span<byte> buffer);
				     }
				     """);

			await That(result.Sources).ContainsKey("Mock.Program_DoSomething1.g.cs");
			await That(result.Sources["Mock.Program_DoSomething1.g.cs"])
				.Contains(
					"global::Mockolate.Setup.ReturnMethodSetup<int, global::Mockolate.Setup.SpanWrapper<byte>>")
				.IgnoringNewlineStyle().And
				.DoesNotContain("NotSupportedException")
				.Because(
					"a by-value Span parameter stays outside the delegate ref-struct carve-out, so the setup surface is emitted against SpanWrapper<T>");
		}

		[Fact]
		public async Task CustomVoidDelegates_ShouldOnlyCreateTwoExtensionsForSetupAndVerify()
		{
			GeneratorResult result = Generator
				.Run("""
				     using System;
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = DoSomething.CreateMock();
				         }

				         public delegate void DoSomething(int x, ref int y, out int z);
				     }
				     """);

			await That(result.Sources).ContainsKey("Mock.Program_DoSomething.g.cs");
			await That(result.Sources["Mock.Program_DoSomething.g.cs"])
				.Contains("""
				          		global::Mockolate.Setup.IVoidMethodSetupWithCallback<int, int, int> global::Mockolate.Mock.IMockSetupForProgram_DoSomething.Setup(global::Mockolate.Parameters.IParameter<int>? x, global::Mockolate.Parameters.IRefParameter<int> y, global::Mockolate.Parameters.IOutParameter<int> z)
				          		{
				          			var methodSetup = new global::Mockolate.Setup.VoidMethodSetup<int, int, int>.WithParameterCollection(MockRegistry, "global::MyCode.Program.DoSomething.Invoke", CovariantParameterAdapter<int>.Wrap(x ?? global::Mockolate.It.IsNull<int>("null")), (global::Mockolate.Parameters.IParameterMatch<int>)(y), (global::Mockolate.Parameters.IParameterMatch<int>)(z));
				          			this.MockRegistry.SetupMethod(global::Mockolate.Mock.Program_DoSomething.MemberId_Invoke, methodSetup);
				          			return methodSetup;
				          		}
				          """).IgnoringNewlineStyle().And
				.Contains("""
				          		global::Mockolate.Setup.IVoidMethodSetupWithCallback<int, int, int> global::Mockolate.Mock.IMockSetupForProgram_DoSomething.Setup(global::Mockolate.Parameters.IParameters parameters)
				          		{
				          			var methodSetup = new global::Mockolate.Setup.VoidMethodSetup<int, int, int>.WithParameters(MockRegistry, "global::MyCode.Program.DoSomething.Invoke", parameters, "x", "y", "z");
				          			this.MockRegistry.SetupMethod(global::Mockolate.Mock.Program_DoSomething.MemberId_Invoke, methodSetup);
				          			return methodSetup;
				          		}
				          """).IgnoringNewlineStyle().And
				.Contains("""
				          		global::Mockolate.Verify.VerificationResult<IMockVerifyForProgram_DoSomething> IMockVerifyForProgram_DoSomething.Verify(global::Mockolate.Parameters.IParameter<int>? x, global::Mockolate.Parameters.IVerifyRefParameter<int> y, global::Mockolate.Parameters.IVerifyOutParameter<int> z)
				          """).IgnoringNewlineStyle().And
				.Contains("""
				          		global::Mockolate.Verify.VerificationResult<IMockVerifyForProgram_DoSomething> IMockVerifyForProgram_DoSomething.Verify(global::Mockolate.Parameters.IParameters parameters)
				          """).IgnoringNewlineStyle();
		}

		[Fact]
		public async Task Delegate_ShouldHaveCorrectReferenceInXMLDocumentation()
		{
			GeneratorResult result = Generator
				.Run("""
				     using System;
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = DoSomething.CreateMock();
				         }

				         public delegate int DoSomething(int x, int y);
				     }
				     """);

			await That(result.Sources).ContainsKey("Mock.Program_DoSomething.g.cs");
			await That(result.Sources["Mock.Program_DoSomething.g.cs"])
				.Contains("Verify invocations for the delegate <see cref=\"global::MyCode.Program.DoSomething\">DoSomething</see> with the given <paramref name=\"x\"/>, <paramref name=\"y\"/>.").And
				.Contains("Verify invocations for the delegate <see cref=\"global::MyCode.Program.DoSomething\">DoSomething</see> with the given <paramref name=\"parameters\"/>.").And
				.DoesNotContain("Verify invocations for the method <see cref=\"global::MyCode.Program.DoSomething.Verify(");
		}

		[Fact]
		public async Task Delegates_ShouldCreateDelegateInsteadOfMockSubject()
		{
			GeneratorResult result = Generator
				.Run("""
				     using System;
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = Func<int, bool>.CreateMock();
				         }
				     }
				     """);

			await That(result.Sources).ContainsKey("Mock.Func_int_bool.g.cs");
			await That(result.Sources["Mock.Func_int_bool.g.cs"])
				.Contains(
					"foreach (global::Mockolate.Setup.ReturnMethodSetup<bool, int> s_methodSetup in this.MockRegistry.GetMethodSetups<global::Mockolate.Setup.ReturnMethodSetup<bool, int>>(\"global::System.Func<int, bool>.Invoke\"))")
				.IgnoringNewlineStyle().And
				.Contains("global::System.Func<int, bool> Object").IgnoringNewlineStyle();
		}

		[Fact]
		public async Task DelegateWithMoreThanMaxParameters_ShouldGenerateSingleAllValueFlagsOverload()
		{
			GeneratorResult result = Generator
				.Run("""
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = ProcessAll.CreateMock();
				         }

				         public delegate int ProcessAll(int a, int b, int c, int d, int e);
				     }
				     """);

			await That(result.Sources).ContainsKey("Mock.Program_ProcessAll.g.cs");
			await That(result.Sources["Mock.Program_ProcessAll.g.cs"])
				.Contains("Setup(int a, int b, int c, int d, int e)")
				.IgnoringNewlineStyle().And
				.Contains("Verify(int a, int b, int c, int d, int e)")
				.IgnoringNewlineStyle().And
				.Contains(
					"Setup(global::Mockolate.Parameters.IParameter<int>? a, global::Mockolate.Parameters.IParameter<int>? b, global::Mockolate.Parameters.IParameter<int>? c, global::Mockolate.Parameters.IParameter<int>? d, global::Mockolate.Parameters.IParameter<int>? e)")
				.IgnoringNewlineStyle();
		}

		[Fact]
		public async Task DelegateWithMoreThanMaxParametersAllOut_ShouldNotGenerateValueFlagsOverload()
		{
			GeneratorResult result = Generator
				.Run("""
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = ProcessAll.CreateMock();
				         }

				         public delegate void ProcessAll(out int a, out int b, out int c, out int d, out int e);
				     }
				     """);

			await That(result.Sources).ContainsKey("Mock.Program_ProcessAll.g.cs");
			await That(result.Sources["Mock.Program_ProcessAll.g.cs"])
				.Contains(
					"Setup(global::Mockolate.Parameters.IOutParameter<int> a, global::Mockolate.Parameters.IOutParameter<int> b, global::Mockolate.Parameters.IOutParameter<int> c, global::Mockolate.Parameters.IOutParameter<int> d, global::Mockolate.Parameters.IOutParameter<int> e)")
				.IgnoringNewlineStyle().And
				.DoesNotContain("Setup(int")
				.IgnoringNewlineStyle();
		}

		[Fact]
		public async Task DelegateWithParameterNamedMethodSetup_ShouldGenerateUniqueLocalVariableName()
		{
			GeneratorResult result = Generator
				.Run("""
				     using System;
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = ProcessResult.CreateMock();
				         }

				         public delegate int ProcessResult(int methodSetup);
				     }
				     """);

			await That(result.Sources).ContainsKey("Mock.Program_ProcessResult.g.cs");
			await That(result.Sources["Mock.Program_ProcessResult.g.cs"])
				.Contains("foreach (global::Mockolate.Setup.ReturnMethodSetup<int, int> s_methodSetup1 in this.MockRegistry.GetMethodSetups<global::Mockolate.Setup.ReturnMethodSetup<int, int>>(")
				.IgnoringNewlineStyle().And
				.Contains("methodSetup1?.TriggerCallbacks(methodSetup);")
				.IgnoringNewlineStyle().And
				.Contains("return methodSetup1?.TryGetReturnValue(methodSetup, out var returnValue) == true ? returnValue : this.MockRegistry.Behavior.DefaultValue.Generate(default(int)!);")
				.IgnoringNewlineStyle();
		}

		[Fact]
		public async Task VoidDelegateWithParameterNamedMethodSetup_ShouldGenerateUniqueLocalVariableName()
		{
			GeneratorResult result = Generator
				.Run("""
				     using System;
				     using Mockolate;

				     namespace MyCode;

				     public class Program
				     {
				         public static void Main(string[] args)
				         {
				     		_ = ProcessResult.CreateMock();
				         }

				         public delegate void ProcessResult(string methodSetup, out int value);
				     }
				     """);

			await That(result.Sources).ContainsKey("Mock.Program_ProcessResult.g.cs");
			await That(result.Sources["Mock.Program_ProcessResult.g.cs"])
				.Contains("foreach (global::Mockolate.Setup.VoidMethodSetup<string, int> s_methodSetup1 in this.MockRegistry.GetMethodSetups<global::Mockolate.Setup.VoidMethodSetup<string, int>>(")
				.IgnoringNewlineStyle().And
				.Contains("methodSetup1?.TriggerCallbacks(methodSetup, value);")
				.IgnoringNewlineStyle();
		}
	}
}
