namespace Mockolate.SourceGenerators.Sources;

internal static partial class Sources
{
	/// <summary>
	///     The <c>AutoMockArguments</c> attribute for NUnit, which replaces <c>[Test]</c> like <c>[TestCase]</c> does.
	/// </summary>
	/// <remarks>
	///     NUnit builds the test cases during discovery and keeps their arguments, so the attribute only reserves them there
	///     and creates them right before the test method runs, also in a repeated or retried test.
	/// </remarks>
	public static string AutoMockArgumentsForNUnit()
		=> AutoMockArgumentsAttribute("MOCKOLATE_NUNIT", "NUnit", "NUnit can run a test method on another thread than its set-up,",
			"global::NUnit.Framework.NUnitAttribute, global::NUnit.Framework.Interfaces.ITestBuilder, global::NUnit.Framework.Interfaces.IImplyFixture, global::NUnit.Framework.Interfaces.IWrapTestMethod",
			ParameterInfoArguments + """
			                         		private static readonly global::System.Runtime.CompilerServices.ConditionalWeakTable<object?[], object?[]> GivenArguments = new();

			                         		/// <inheritdoc />
			                         		public global::System.Collections.Generic.IEnumerable<global::NUnit.Framework.Internal.TestMethod> BuildFrom(
			                         			global::NUnit.Framework.Interfaces.IMethodInfo method, global::NUnit.Framework.Internal.Test? suite)
			                         		{
			                         			global::System.Reflection.ParameterInfo[] parameters = method.MethodInfo.GetParameters();
			                         			int parameterCount = parameters.Length;
			                         			// NUnit 4 passes its own token to a last CancellationToken parameter that gets no argument.
			                         			if (typeof(global::NUnit.Framework.NUnitAttribute).Assembly.GetName().Version?.Major >= 4 &&
			                         			    parameterCount > Arguments.Length &&
			                         			    parameters[parameterCount - 1].ParameterType == typeof(global::System.Threading.CancellationToken))
			                         			{
			                         				parameterCount--;
			                         			}
			                         			global::NUnit.Framework.Internal.TestCaseParameters testCase;
			                         			if (Arguments.Length > parameterCount)
			                         			{
			                         				testCase = new global::NUnit.Framework.Internal.TestCaseParameters(new global::Mockolate.Exceptions.MockException(
			                         					$"AutoMockArguments has {Arguments.Length} arguments, but the test method has only {parameterCount} parameters."));
			                         			}
			                         			else
			                         			{
			                         				object?[] reserved = new object?[parameterCount];
			                         				global::System.Array.Copy(Arguments, reserved, Arguments.Length);
			                         				testCase = new global::NUnit.Framework.Internal.TestCaseParameters(reserved);
			                         			}

			                         			// NUnit would read braces in the given arguments as a pattern, so the name is set on the built test.
			                         			testCase.TestName = method.Name;
			                         			global::NUnit.Framework.Internal.TestMethod test =
			                         				new global::NUnit.Framework.Internal.Builders.NUnitTestCaseBuilder().BuildTestMethod(method, suite, testCase);
			                         			test.Name = global::Mockolate.Integrations.AutoMockRow.GetDisplayName(method.Name, Arguments);
			                         			test.FullName = $"{suite?.FullName ?? method.TypeInfo.FullName}.{test.Name}";
			                         			// A test case that is not runnable has no arguments with NUnit before 3.13, and shares an empty array otherwise.
			                         			if (test.Arguments is { Length: > 0, } reservedArguments)
			                         			{
			                         				GivenArguments.Add(reservedArguments, Arguments);
			                         			}

			                         			yield return test;
			                         		}

			                         		/// <summary>
			                         		///     Every <c>AutoMockArguments</c> attribute on the method wraps each of its test cases, so a command that already
			                         		///     creates the arguments is kept as it is.
			                         		/// </summary>
			                         		/// <remarks>
			                         		///     The command of the test method itself is replaced, so that the arguments are created right before the method
			                         		///     runs and other wrappers, such as the execution hooks of NUnit 4.5, still get a command of the test method.
			                         		/// </remarks>
			                         		public global::NUnit.Framework.Internal.Commands.TestCommand Wrap(
			                         			global::NUnit.Framework.Internal.Commands.TestCommand command)
			                         			=> command switch
			                         			{
			                         				CreateArgumentsTestMethodCommand or CreateArgumentsCommand => command,
			                         				// A subclass of another attribute runs code of its own, so only the command of NUnit is replaced.
			                         				global::NUnit.Framework.Internal.Commands.TestMethodCommand { Test: global::NUnit.Framework.Internal.TestMethod test, }
			                         					when command.GetType() == typeof(global::NUnit.Framework.Internal.Commands.TestMethodCommand) =>
			                         					new CreateArgumentsTestMethodCommand(test),
			                         				_ => new CreateArgumentsCommand(command),
			                         			};

			                         		/// <summary>
			                         		///     Creates the arguments of the test after the set-up, again for every repeated or retried run.
			                         		/// </summary>
			                         		private static void CreateArguments(global::NUnit.Framework.Internal.Test test)
			                         		{
			                         			if (test is global::NUnit.Framework.Internal.TestMethod testMethod &&
			                         			    GivenArguments.TryGetValue(testMethod.Arguments, out object?[]? given))
			                         			{
			                         				// The method of the test is the constructed one of a generic test method. A last parameter without an
			                         				// argument gets the token of NUnit 4.
			                         				global::System.Reflection.ParameterInfo[] parameters = testMethod.Method.MethodInfo.GetParameters();
			                         				global::System.Array.Resize(ref parameters, testMethod.Arguments.Length);
			                         				object?[] values = CreateArguments(parameters, given);
			                         				// The given arguments keep the conversions of NUnit.
			                         				global::System.Array.Copy(values, given.Length, testMethod.Arguments, given.Length, values.Length - given.Length);
			                         			}
			                         		}

			                         		private sealed class CreateArgumentsTestMethodCommand(global::NUnit.Framework.Internal.TestMethod testMethod)
			                         			: global::NUnit.Framework.Internal.Commands.TestMethodCommand(testMethod)
			                         		{
			                         			/// <inheritdoc />
			                         			public override global::NUnit.Framework.Internal.TestResult Execute(
			                         				global::NUnit.Framework.Internal.TestExecutionContext context)
			                         			{
			                         				CreateArguments(Test);
			                         				return base.Execute(context);
			                         			}
			                         		}

			                         		private sealed class CreateArgumentsCommand(global::NUnit.Framework.Internal.Commands.TestCommand innerCommand)
			                         			: global::NUnit.Framework.Internal.Commands.DelegatingTestCommand(innerCommand)
			                         		{
			                         			/// <inheritdoc />
			                         			public override global::NUnit.Framework.Internal.TestResult Execute(
			                         				global::NUnit.Framework.Internal.TestExecutionContext context)
			                         			{
			                         				CreateArguments(Test);
			                         				return innerCommand.Execute(context);
			                         			}
			                         		}
			                         """);
}
