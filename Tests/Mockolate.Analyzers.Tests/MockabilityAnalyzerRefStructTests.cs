using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verifier = Mockolate.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<Mockolate.Analyzers.MockabilityAnalyzer>;

namespace Mockolate.Analyzers.Tests;

/// <summary>
///     Coverage for the <c>Mockolate0003</c> ref-struct mockability diagnostic emitted by
///     <see cref="MockabilityAnalyzer" />.
/// </summary>
/// <remarks>
///     The analyzer-test host targets net10.0 with Mockolate's ref-struct types referenced, so
///     the "target framework lacks the pipeline" branch of
///     <c>GetRefStructPipelineUnsupportedReason</c> cannot be exercised here. The C# language
///     version branch (<c>LangVersion &lt; 13</c>) is reachable via the LanguageVersion-aware
///     verifier overload. All other failure modes (out/ref ref-struct params, non-span ref-struct
///     returns, ref-struct-keyed indexers, delegates, inheritance) are covered below.
/// </remarks>
public class MockabilityAnalyzerRefStructTests
{
	[Fact]
	public async Task WhenLanguageVersionBelowCSharp13_RefReadonlySpanParameterMethod_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.ISpanInspector")}}

			  namespace MyNamespace
			  {
			  	public interface ISpanInspector
			  	{
			  		// `ref readonly` Span has no wrapper-based emit branch on the interface pipeline and
			  		// falls back to the generic ref-struct path, so it needs .NET 9 / C# 13 even though
			  		// the same parameter by value would not.
			  		void Inspect(ref readonly System.Span<int> values);
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			{|#0:ISpanInspector|}.CreateMock();
			  		}
			  	}
			  }
			  """,
			LanguageVersion.CSharp12,
			new DiagnosticResult("Mockolate0003", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("MyNamespace.ISpanInspector", "Inspect",
					"ref-struct parameter mocking requires C# 13 or later (uses the 'allows ref struct' anti-constraint; current LangVersion is 12.0)")
		);

	[Fact]
	public async Task WhenLanguageVersionBelowCSharp13_RefStructKeyedIndexer_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.IRefStructLookup")}}

			  namespace MyNamespace
			  {
			  	public readonly ref struct Key(int id) { public int Id { get; } = id; }

			  	public interface IRefStructLookup
			  	{
			  		string this[Key key] { get; }
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			{|#0:IRefStructLookup|}.CreateMock();
			  		}
			  	}
			  }
			  """,
			LanguageVersion.CSharp12,
			new DiagnosticResult("Mockolate0003", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("MyNamespace.IRefStructLookup", "this[]",
					"ref-struct-keyed indexers require C# 13 or later (uses the 'allows ref struct' anti-constraint; current LangVersion is 12.0)")
		);

	[Fact]
	public async Task WhenLanguageVersionBelowCSharp13_RefStructParameterMethod_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.IPacketSink")}}

			  namespace MyNamespace
			  {
			  	public readonly ref struct Packet(int id) { public int Id { get; } = id; }

			  	public interface IPacketSink
			  	{
			  		void Consume(Packet packet);
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			{|#0:IPacketSink|}.CreateMock();
			  		}
			  	}
			  }
			  """,
			LanguageVersion.CSharp12,
			new DiagnosticResult("Mockolate0003", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("MyNamespace.IPacketSink", "Consume",
					"ref-struct parameter mocking requires C# 13 or later (uses the 'allows ref struct' anti-constraint; current LangVersion is 12.0)")
		);

	[Fact]
	public async Task WhenMockingAbstractClassWithInheritedRefStructOutParameter_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.DerivedProducer")}}

			  namespace MyNamespace
			  {
			  	public readonly ref struct Packet(int id) { public int Id { get; } = id; }

			  	public abstract class BaseProducer
			  	{
			  		// Out ref-struct parameters are supported via IOutRefStructParameter<T>.
			  		public abstract void Produce(out Packet packet);
			  	}

			  	public abstract class DerivedProducer : BaseProducer
			  	{
			  		public abstract void Extra();
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			DerivedProducer.CreateMock();
			  		}
			  	}
			  }
			  """
		);

	[Fact]
	public async Task WhenMockingAbstractClassWithRefStructProperty_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.AbstractPacketSource")}}

			  namespace MyNamespace
			  {
			  	public readonly ref struct Packet(int id) { public int Id { get; } = id; }

			  	public abstract class AbstractPacketSource
			  	{
			  		public abstract Packet Current { get; }
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			{|#0:AbstractPacketSource|}.CreateMock();
			  		}
			  	}
			  }
			  """,
			new DiagnosticResult("Mockolate0003", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("MyNamespace.AbstractPacketSource", "Current",
					"properties of a non-span ref struct type are not supported")
		);

	[Fact]
	public async Task WhenMockingClassWithVirtualInitOnlyRefStructProperty_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.InitPacketSource")}}

			  namespace MyNamespace
			  {
			  	public readonly ref struct Packet(int id) { public int Id { get; } = id; }

			  	public class InitPacketSource
			  	{
			  		private int _id;

			  		// The getter would forward, but an init-only accessor cannot assign through the
			  		// wrapped instance and keeps the NotSupportedException stub.
			  		public virtual Packet Current
			  		{
			  			get => new Packet(_id);
			  			init => _id = value.Id;
			  		}
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			{|#0:InitPacketSource|}.CreateMock();
			  		}
			  	}
			  }
			  """,
			new DiagnosticResult("Mockolate0003", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("MyNamespace.InitPacketSource", "Current",
					"properties of a non-span ref struct type are not supported")
		);

	[Fact]
	public async Task WhenMockingClassWithVirtualRefReturningRefStructMethod_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.PacketPicker")}}

			  namespace MyNamespace
			  {
			  	public readonly ref struct Packet(int id) { public int Id { get; } = id; }

			  	public class PacketPicker
			  	{
			  		// `return base.Pick(ref packet)` is not valid for a by-ref return, so this keeps
			  		// the stub rather than forwarding.
			  		public virtual ref Packet Pick(ref Packet packet) => ref packet;
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			{|#0:PacketPicker|}.CreateMock();
			  		}
			  	}
			  }
			  """,
			new DiagnosticResult("Mockolate0003", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("MyNamespace.PacketPicker", "Pick",
					"methods returning a non-span ref struct are not supported")
		);

	[Fact]
	public async Task WhenMockingClassWithVirtualRefStructIndexer_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.PacketCatalog")}}

			  namespace MyNamespace
			  {
			  	public readonly ref struct Packet(int id) { public int Id { get; } = id; }

			  	public class PacketCatalog
			  	{
			  		public virtual Packet this[int index] => new Packet(index);
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			PacketCatalog.CreateMock();
			  		}
			  	}
			  }
			  """
		);

	[Fact]
	public async Task WhenMockingClassWithVirtualRefStructProperty_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.PacketSource")}}

			  namespace MyNamespace
			  {
			  	public readonly ref struct Packet(int id) { public int Id { get; } = id; }

			  	public class PacketSource
			  	{
			  		// No setup surface, but the override forwards to the wrapped instance or to base,
			  		// so the mock keeps behaving like the real member and there is nothing to fix.
			  		public virtual Packet Current => new Packet(11);
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			PacketSource.CreateMock();
			  		}
			  	}
			  }
			  """
		);

	[Fact]
	public async Task WhenMockingClassWithVirtualRefStructReturnAndParameter_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.PacketTransformer")}}

			  namespace MyNamespace
			  {
			  	public readonly ref struct Packet(int id) { public int Id { get; } = id; }

			  	public class PacketTransformer
			  	{
			  		// The forwarding return branch wins over the parameter pipeline, so the
			  		// ref-struct parameter must not be reported either.
			  		public virtual Packet Transform(Packet packet) => packet;
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			PacketTransformer.CreateMock();
			  		}
			  	}
			  }
			  """
		);

	[Fact]
	public async Task WhenMockingClassWithVirtualRefStructReturningMethod_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.PacketProducer")}}

			  namespace MyNamespace
			  {
			  	public readonly ref struct Packet(int id) { public int Id { get; } = id; }

			  	public class PacketProducer
			  	{
			  		public virtual Packet Produce() => new Packet(12);
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			PacketProducer.CreateMock();
			  		}
			  	}
			  }
			  """
		);

	[Fact]
	public async Task WhenMockingDelegateReturningNonSpanRefStruct_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.PacketFactoryDelegate")}}

			  namespace MyNamespace
			  {
			  	public readonly ref struct Packet(int id) { public int Id { get; } = id; }

			  	// Delegate returning a non-span ref struct — same rejection as interface
			  	// methods with that return shape.
			  	public delegate Packet PacketFactoryDelegate();

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			{|#0:PacketFactoryDelegate|}.CreateMock();
			  		}
			  	}
			  }
			  """,
			new DiagnosticResult("Mockolate0003", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("MyNamespace.PacketFactoryDelegate", "Invoke",
					"methods returning a non-span ref struct are not supported")
		);

	[Fact]
	public async Task WhenMockingDelegateWithOutRefStructParameter_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.PacketProducerDelegate")}}

			  namespace MyNamespace
			  {
			  	public readonly ref struct Packet(int id) { public int Id { get; } = id; }

			  	// Delegate Invoke methods are analyzed the same as interface methods; ref-struct
			  	// parameters (any RefKind) must be rejected because the emitted VoidMethodSetup
			  	// has no 'allows ref struct' constraint.
			  	public delegate void PacketProducerDelegate(out Packet packet);

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			{|#0:PacketProducerDelegate|}.CreateMock();
			  		}
			  	}
			  }
			  """,
			new DiagnosticResult("Mockolate0003", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("MyNamespace.PacketProducerDelegate", "Invoke",
					"ref-struct parameters are not supported on delegate types")
		);

	[Fact]
	public async Task WhenMockingDelegateWithPlainRefStructParameter_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.PacketHandler")}}

			  namespace MyNamespace
			  {
			  	public readonly ref struct Packet(int id) { public int Id { get; } = id; }

			  	// Delegate Invoke with a plain ref-struct parameter — the generator emits
			  	// VoidMethodSetup<Packet>, which lacks 'allows ref struct' and therefore fails
			  	// to compile. The analyzer must reject this case as well.
			  	public delegate void PacketHandler(Packet packet);

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			{|#0:PacketHandler|}.CreateMock();
			  		}
			  	}
			  }
			  """,
			new DiagnosticResult("Mockolate0003", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("MyNamespace.PacketHandler", "Invoke",
					"ref-struct parameters are not supported on delegate types")
		);

	[Fact]
	public async Task WhenMockingDelegateWithRefReadonlySpanParameter_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.SpanInspector")}}

			  namespace MyNamespace
			  {
			  	// `ref readonly` Span is the one span ref kind with no wrapper-based emit branch, so it
			  	// reaches the delegate carve-out even though the same parameter by value would not.
			  	public delegate void SpanInspector(ref readonly System.Span<int> values);

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			{|#0:SpanInspector|}.CreateMock();
			  		}
			  	}
			  }
			  """,
			new DiagnosticResult("Mockolate0003", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("MyNamespace.SpanInspector", "Invoke",
					"ref-struct parameters are not supported on delegate types")
		);

	[Fact]
	public async Task WhenMockingInterfaceInheritingRefStructOutMethod_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.IDerivedSink")}}

			  namespace MyNamespace
			  {
			  	public readonly ref struct Packet(int id) { public int Id { get; } = id; }

			  	public interface IBaseSink
			  	{
			  		void Produce(out Packet packet);
			  	}

			  	public interface IDerivedSink : IBaseSink
			  	{
			  		void Extra();
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			IDerivedSink.CreateMock();
			  		}
			  	}
			  }
			  """
		);

	[Fact]
	public async Task WhenMockingInterfaceReturningNonSpanRefStruct_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.IPacketFactory")}}

			  namespace MyNamespace
			  {
			  	public readonly ref struct Packet(int id) { public int Id { get; } = id; }

			  	public interface IPacketFactory
			  	{
			  		Packet Produce();
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			{|#0:IPacketFactory|}.CreateMock();
			  		}
			  	}
			  }
			  """,
			new DiagnosticResult("Mockolate0003", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("MyNamespace.IPacketFactory", "Produce",
					"methods returning a non-span ref struct are not supported")
		);

	[Fact]
	public async Task WhenMockingInterfaceWithOutRefStructParameter_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.IPacketProducer")}}

			  namespace MyNamespace
			  {
			  	public readonly ref struct Packet(int id) { public int Id { get; } = id; }

			  	public interface IPacketProducer
			  	{
			  		void Produce(out Packet packet);
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			IPacketProducer.CreateMock();
			  		}
			  	}
			  }
			  """
		);

	[Fact]
	public async Task WhenMockingInterfaceWithRefStructOutAndPlainOverloads_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.IOverloadedSink")}}

			  namespace MyNamespace
			  {
			  	public readonly ref struct Packet(int id) { public int Id { get; } = id; }

			  	// Both overloads are now supported — the by-value variant via the standard
			  	// ref-struct pipeline, the out variant via IOutRefStructParameter<T>.
			  	public interface IOverloadedSink
			  	{
			  		void Consume(Packet packet);
			  		void Consume(out Packet packet);
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			IOverloadedSink.CreateMock();
			  		}
			  	}
			  }
			  """
		);

	[Fact]
	public async Task WhenMockingInterfaceWithPlainRefStructParameter_ShouldNotFlag() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.IPacketSink")}}

			  namespace MyNamespace
			  {
			  	public readonly ref struct Packet(int id) { public int Id { get; } = id; }

			  	public interface IPacketSink
			  	{
			  		void Consume(Packet packet);
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			IPacketSink.CreateMock();
			  		}
			  	}
			  }
			  """
		);

	[Fact]
	public async Task WhenMockingInterfaceWithRefReadonlyRefStructParameter_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.IPacketInspector")}}

			  namespace MyNamespace
			  {
			  	public readonly ref struct Packet(int id) { public int Id { get; } = id; }

			  	public interface IPacketInspector
			  	{
			  		void Inspect(ref readonly Packet packet);
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			IPacketInspector.CreateMock();
			  		}
			  	}
			  }
			  """
		);

	[Fact]
	public async Task WhenMockingInterfaceWithRefReadonlySpanParameter_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.ISpanInspector")}}

			  namespace MyNamespace
			  {
			  	public interface ISpanInspector
			  	{
			  		// Routes through the ref-struct pipeline, which this compilation hosts — so the
			  		// parameter-level rule must not report on a supported target either.
			  		void Inspect(ref readonly System.Span<int> values);
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			ISpanInspector.CreateMock();
			  		}
			  	}
			  }
			  """
		);

	[Fact]
	public async Task WhenMockingInterfaceWithRefRefStructParameter_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.IPacketMutator")}}

			  namespace MyNamespace
			  {
			  	public readonly ref struct Packet(int id) { public int Id { get; } = id; }

			  	public interface IPacketMutator
			  	{
			  		void Mutate(ref Packet packet);
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			IPacketMutator.CreateMock();
			  		}
			  	}
			  }
			  """
		);

	[Fact]
	public async Task WhenMockingInterfaceWithRefStructIndexerKey_GetterOnly_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.IRefStructLookup")}}

			  namespace MyNamespace
			  {
			  	public readonly ref struct Key(int id) { public int Id { get; } = id; }

			  	public interface IRefStructLookup
			  	{
			  		// Getter-only: fully supported via the ref-struct indexer pipeline.
			  		string this[Key key] { get; }
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			IRefStructLookup.CreateMock();
			  		}
			  	}
			  }
			  """
		);

	[Fact]
	public async Task WhenMockingInterfaceWithRefStructIndexerKeyAndSetter_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.IRefStructStore")}}

			  namespace MyNamespace
			  {
			  	public readonly ref struct Key(int id) { public int Id { get; } = id; }

			  	public interface IRefStructStore
			  	{
			  		// Get+set ref-struct-keyed indexers route through the combined
			  		// IRefStructIndexerSetup facade.
			  		string this[Key key] { get; set; }
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			IRefStructStore.CreateMock();
			  		}
			  	}
			  }
			  """
		);

	[Fact]
	public async Task WhenMockingInterfaceWithSpanParameter_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.ISpanConsumer")}}

			  namespace MyNamespace
			  {
			  	public interface ISpanConsumer
			  	{
			  		// Span/ReadOnlySpan parameters go through the SpanWrapper/ReadOnlySpanWrapper
			  		// carve-out; they're not routed through the ref-struct pipeline.
			  		void Consume(System.ReadOnlySpan<byte> payload);
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			ISpanConsumer.CreateMock();
			  		}
			  	}
			  }
			  """
		);

	[Fact]
	public async Task WhenMockingInterfaceWithSpanReturn_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.IPayloadSource")}}

			  namespace MyNamespace
			  {
			  	public interface IPayloadSource
			  	{
			  		// Span/ReadOnlySpan returns go through the existing wrapper pipeline; no
			  		// ref-struct diagnostic is warranted.
			  		System.ReadOnlySpan<byte> GetPayload();
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			IPayloadSource.CreateMock();
			  		}
			  	}
			  }
			  """
		);

	[Fact]
	public async Task WhenMockingInterfaceWithRefStructProperty_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.IPacketHolder")}}

			  namespace MyNamespace
			  {
			  	public readonly ref struct Packet(int id) { public int Id { get; } = id; }

			  	public interface IPacketHolder
			  	{
			  		Packet Current { get; }
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			{|#0:IPacketHolder|}.CreateMock();
			  		}
			  	}
			  }
			  """,
			new DiagnosticResult("Mockolate0003", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("MyNamespace.IPacketHolder", "Current",
					"properties of a non-span ref struct type are not supported")
		);

	[Fact]
	public async Task WhenMockingInterfaceWithSpanProperty_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.ISpanBuffer")}}

			  namespace MyNamespace
			  {
			  	public interface ISpanBuffer
			  	{
			  		System.Span<byte> Buffer { get; set; }
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			ISpanBuffer.CreateMock();
			  		}
			  	}
			  }
			  """
		);

	[Fact]
	public async Task WhenMockingInterfaceWithSpanValuedIndexer_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.ISpanCatalog")}}

			  namespace MyNamespace
			  {
			  	public interface ISpanCatalog
			  	{
			  		// Span-valued indexers round-trip through SpanWrapper<T> on both accessors.
			  		System.Span<byte> this[int index] { get; set; }
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			ISpanCatalog.CreateMock();
			  		}
			  	}
			  }
			  """
		);

	[Fact]
	public async Task WhenMockingInterfaceWithRefStructValuedIndexer_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.IPacketCatalog")}}

			  namespace MyNamespace
			  {
			  	public readonly ref struct Packet(int id) { public int Id { get; } = id; }

			  	public interface IPacketCatalog
			  	{
			  		Packet this[int index] { get; }
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			{|#0:IPacketCatalog|}.CreateMock();
			  		}
			  	}
			  }
			  """,
			new DiagnosticResult("Mockolate0003", DiagnosticSeverity.Warning)
				.WithLocation(0)
				.WithArguments("MyNamespace.IPacketCatalog", "this[]",
					"indexers returning a non-span ref struct are not supported")
		);

	[Fact]
	public async Task WhenMockingInterfaceWithRefStructEventArgument_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  {{GeneratedPrefix("MyNamespace.IPacketNotifier")}}

			  namespace MyNamespace
			  {
			  	public readonly ref struct Packet(int id) { public int Id { get; } = id; }

			  	public delegate void PacketEventHandler(Packet packet);

			  	public interface IPacketNotifier
			  	{
			  		event PacketEventHandler PacketReceived;
			  	}

			  	public class MyClass
			  	{
			  		public void MyTest()
			  		{
			  			IPacketNotifier.CreateMock();
			  		}
			  	}
			  }
			  """
		);

	private static string GeneratedPrefix(string fullyQualifiedTypeName)
	{
		string simpleName = fullyQualifiedTypeName.Split('.')[^1];
		return $$"""
		         namespace Mockolate
		         {
		         	internal static partial class MockExtensionsFor{{simpleName}}
		         	{
		         		extension({{fullyQualifiedTypeName}} mock)
		         		{
		         			public static {{fullyQualifiedTypeName}} CreateMock() => default!;
		         		}
		         	}
		         }
		         """;
	}
}
