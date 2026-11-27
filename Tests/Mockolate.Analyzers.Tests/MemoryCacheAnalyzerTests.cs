using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Verifier = Mockolate.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<Mockolate.Analyzers.MemoryCacheAnalyzer>;

namespace Mockolate.Analyzers.Tests;

public class MemoryCacheAnalyzerTests
{
	private const string Prefix = """
	                              #nullable enable
	                              using Microsoft.Extensions.Caching.Memory;
	                              using Mockolate;
	                              using Mockolate.Setup;
	                              using Mockolate.Verify;

	                              namespace Microsoft.Extensions.Caching.Memory
	                              {
	                              	public interface IMemoryCache
	                              	{
	                              		bool TryGetValue(object key, out object? value);
	                              		object CreateEntry(object key);
	                              		void Remove(object key);
	                              	}
	                              }

	                              namespace Mockolate
	                              {
	                              	internal static class MockolateCaching
	                              	{
	                              		extension(IMockSetup<IMemoryCache> setup)
	                              		{
	                              			public object? Get(object key) => null;
	                              		}

	                              		extension<TVerify>(TVerify verify) where TVerify : class, IMockVerify<IMemoryCache>
	                              		{
	                              			public object? Set(object key, object? value) => null;
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
	public async Task WhenClassHasBaseImplementations_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			Prefix + """
			         namespace MyCode
			         {
			         	public class MyCache : IMemoryCache
			         	{
			         		public virtual bool TryGetValue(object key, out object? value) { value = null; return false; }
			         		public virtual object CreateEntry(object key) => null!;
			         		public virtual void Remove(object key) { }
			         	}

			         	public class MyClass
			         	{
			         		public void MyTest(ISetup<MyCache> setup, IVerify<MyCache> verify)
			         		{
			         			_ = setup.{|#0:Get|}("key");
			         			_ = verify.{|#1:Set|}("key", null);
			         		}
			         	}
			         }
			         """,
			new DiagnosticResult("Mockolate0015", DiagnosticSeverity.Error)
				.WithLocation(0).WithArguments("MyCode.MyCache"),
			new DiagnosticResult("Mockolate0015", DiagnosticSeverity.Error)
				.WithLocation(1).WithArguments("MyCode.MyCache"));

	[Test]
	public async Task WhenOnlySomeMembersAreAbstract_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			Prefix + """
			         namespace MyCode
			         {
			         	public abstract class MyCache : IMemoryCache
			         	{
			         		public abstract bool TryGetValue(object key, out object? value);
			         		public abstract object CreateEntry(object key);
			         		public virtual void Remove(object key) { }
			         	}

			         	public class MyClass
			         	{
			         		public void MyTest(ISetup<MyCache> setup)
			         		{
			         			_ = setup.{|#0:Get|}("key");
			         		}
			         	}
			         }
			         """,
			new DiagnosticResult("Mockolate0015", DiagnosticSeverity.Error)
				.WithLocation(0).WithArguments("MyCode.MyCache"));

	[Test]
	public async Task WhenAbstractMemberIsOverriddenInDerivedClass_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			Prefix + """
			         namespace MyCode
			         {
			         	public abstract class MyCacheBase : IMemoryCache
			         	{
			         		public abstract bool TryGetValue(object key, out object? value);
			         		public abstract object CreateEntry(object key);
			         		public abstract void Remove(object key);
			         	}

			         	public abstract class MyCache : MyCacheBase
			         	{
			         		public override void Remove(object key) { }
			         	}

			         	public class MyClass
			         	{
			         		public void MyTest(ISetup<MyCache> setup)
			         		{
			         			_ = setup.{|#0:Get|}("key");
			         		}
			         	}
			         }
			         """,
			new DiagnosticResult("Mockolate0015", DiagnosticSeverity.Error)
				.WithLocation(0).WithArguments("MyCode.MyCache"));

	[Test]
	public async Task WhenAllMembersAreAbstract_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			Prefix + """
			         namespace MyCode
			         {
			         	public abstract class MyCache : IMemoryCache
			         	{
			         		public abstract bool TryGetValue(object key, out object? value);
			         		public abstract object CreateEntry(object key);
			         		public abstract void Remove(object key);
			         	}

			         	public class MyClass
			         	{
			         		public void MyTest(ISetup<MyCache> setup, IVerify<MyCache> verify)
			         		{
			         			_ = setup.Get("key");
			         			_ = verify.Set("key", null);
			         		}
			         	}
			         }
			         """);

	[Test]
	public async Task WhenFacadeAlsoCoversClassWithoutCache_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			Prefix + """
			         namespace MyCode
			         {
			         	public class Other { }

			         	public abstract class MyCache : IMemoryCache
			         	{
			         		public abstract bool TryGetValue(object key, out object? value);
			         		public abstract object CreateEntry(object key);
			         		public abstract void Remove(object key);
			         	}

			         	public interface IBoth : ISetup<Other>, ISetup<MyCache> { }

			         	public class MyClass
			         	{
			         		public void MyTest(IBoth setup)
			         		{
			         			_ = setup.Get("key");
			         		}
			         	}
			         }
			         """);

	[Test]
	public async Task WhenMockIsAnInterface_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			Prefix + """
			         namespace MyCode
			         {
			         	public interface IOrderCache : IMemoryCache { }

			         	public class MyCache : IMemoryCache
			         	{
			         		public virtual bool TryGetValue(object key, out object? value) { value = null; return false; }
			         		public virtual object CreateEntry(object key) => null!;
			         		public virtual void Remove(object key) { }
			         	}

			         	public class MyClass
			         	{
			         		public void MyTest(ISetup<IMemoryCache> setup, IVerify<IOrderCache> verify, ISetup<MyCache> classSetup)
			         		{
			         			_ = setup.Get("key");
			         			_ = verify.Set("key", null);
			         			_ = ((IMockSetup<IMemoryCache>)classSetup).Get("key");
			         		}
			         	}
			         }
			         """);

	[Test]
	public async Task WhenMethodIsNotACacheMember_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			Prefix + """
			         namespace MyCode
			         {
			         	public static class OtherExtensions
			         	{
			         		public static object? Get(this IMockSetup<IMemoryCache> setup, int key) => null;
			         	}

			         	public class MyCache : IMemoryCache
			         	{
			         		public virtual bool TryGetValue(object key, out object? value) { value = null; return false; }
			         		public virtual object CreateEntry(object key) => null!;
			         		public virtual void Remove(object key) { }
			         	}

			         	public class MyClass
			         	{
			         		public void MyTest(ISetup<MyCache> setup)
			         		{
			         			_ = setup.Get(42);
			         		}
			         	}
			         }
			         """);
}
