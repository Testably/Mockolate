using BenchmarkDotNet.Attributes;
using FakeItEasy;
using Imposter.Abstractions;
using Mockolate.Benchmarks;
using Mockolate.Setup;
using Mockolate.Verify;
using NSubstitute;
using Times = Moq.Times;

[assembly: GenerateImposter(typeof(SequenceBenchmarks.IChocolateOven))]

namespace Mockolate.Benchmarks;
#pragma warning disable CA1822 // Mark members as static
/// <summary>
///     In this benchmark we check an interface mock with a return sequence: a method returns two values and then throws,
///     and a second method gets another return value after its first call. No method is called more often than it has
///     return values, because the libraries behave differently once a sequence is used up.
/// </summary>
public class SequenceBenchmarks : BenchmarksBase
{
	/// <summary>
	///     <see href="https://docs.testably.org/Mockolate" />
	/// </summary>
	[Benchmark(Baseline = true)]
	public int Sequence_Mockolate()
	{
		IChocolateOven sut = IChocolateOven.CreateMock();
		sut.Mock.Setup.NextBatch().Returns(1).Returns(2).Throws<InvalidOperationException>();
		IReturnMethodSetupReturnBuilder<int> temperature = sut.Mock.Setup.Temperature().Returns(180);

		int sum = sut.NextBatch() + sut.NextBatch();
		try
		{
			sut.NextBatch();
		}
		catch (InvalidOperationException)
		{
			sum++;
		}

		sum += sut.Temperature();
		temperature.Returns(200);
		sum += sut.Temperature();

		sut.Mock.Verify.NextBatch().Exactly(3);
		return sum;
	}

	/// <summary>
	///     <see href="https://github.com/themidnightgospel/Imposter" />
	/// </summary>
	[Benchmark]
	public int Sequence_Imposter()
	{
		IChocolateOvenImposter imposter = IChocolateOven.Imposter();
		imposter.NextBatch().Returns(1).Then().Returns(2).Then().Throws<InvalidOperationException>();
		imposter.Temperature().Returns(180);
		IChocolateOven sut = imposter.Instance();

		int sum = sut.NextBatch() + sut.NextBatch();
		try
		{
			sut.NextBatch();
		}
		catch (InvalidOperationException)
		{
			sum++;
		}

		sum += sut.Temperature();
		imposter.Temperature().Returns(200);
		sum += sut.Temperature();

		imposter.NextBatch().Called(Count.Exactly(3));
		return sum;
	}

	/// <summary>
	///     <see href="https://github.com/thomhurst/TUnit/" />
	/// </summary>
	[Benchmark]
	public int Sequence_TUnitMocks()
	{
		SequenceBenchmarks_IChocolateOvenMock mock = IChocolateOven.Mock();
		mock.NextBatch().Returns(1).Then().Returns(2).Then().Throws<InvalidOperationException>();
		mock.Temperature().Returns(180);
		IChocolateOven sut = mock.Object;

		int sum = sut.NextBatch() + sut.NextBatch();
		try
		{
			sut.NextBatch();
		}
		catch (InvalidOperationException)
		{
			sum++;
		}

		sum += sut.Temperature();
		mock.Temperature().Returns(200);
		sum += sut.Temperature();

		mock.NextBatch().WasCalled(TUnit.Mocks.Times.Exactly(3));
		return sum;
	}

	/// <summary>
	///     <see href="https://github.com/devlooped/moq" />
	/// </summary>
	[Benchmark]
	public int Sequence_Moq()
	{
		Moq.Mock<IChocolateOven> mock = new();
		mock.SetupSequence(x => x.NextBatch()).Returns(1).Returns(2).Throws<InvalidOperationException>();
		mock.Setup(x => x.Temperature()).Returns(180);
		IChocolateOven sut = mock.Object;

		int sum = sut.NextBatch() + sut.NextBatch();
		try
		{
			sut.NextBatch();
		}
		catch (InvalidOperationException)
		{
			sum++;
		}

		sum += sut.Temperature();
		mock.Setup(x => x.Temperature()).Returns(200);
		sum += sut.Temperature();

		mock.Verify(x => x.NextBatch(), Times.Exactly(3));
		return sum;
	}

	/// <summary>
	///     <see href="https://nsubstitute.github.io/" />
	/// </summary>
	[Benchmark]
	public int Sequence_NSubstitute()
	{
		IChocolateOven mock = Substitute.For<IChocolateOven>();
		mock.NextBatch().Returns(_ => 1, _ => 2, _ => throw new InvalidOperationException());
		mock.Temperature().Returns(180);

		int sum = mock.NextBatch() + mock.NextBatch();
		try
		{
			mock.NextBatch();
		}
		catch (InvalidOperationException)
		{
			sum++;
		}

		sum += mock.Temperature();
		mock.Temperature().Returns(200);
		sum += mock.Temperature();

		mock.Received(3).NextBatch();
		return sum;
	}

	/// <summary>
	///     <see href="https://fakeiteasy.github.io/" />
	/// </summary>
	[Benchmark]
	public int Sequence_FakeItEasy()
	{
		IChocolateOven mock = A.Fake<IChocolateOven>();
		A.CallTo(() => mock.NextBatch()).Returns(1).Once().Then.Returns(2).Once().Then.Throws<InvalidOperationException>();
		A.CallTo(() => mock.Temperature()).Returns(180);

		int sum = mock.NextBatch() + mock.NextBatch();
		try
		{
			mock.NextBatch();
		}
		catch (InvalidOperationException)
		{
			sum++;
		}

		sum += mock.Temperature();
		A.CallTo(() => mock.Temperature()).Returns(200);
		sum += mock.Temperature();

		A.CallTo(() => mock.NextBatch()).MustHaveHappened(3, FakeItEasy.Times.Exactly);
		return sum;
	}

	public interface IChocolateOven
	{
		int NextBatch();
		int Temperature();
	}
}
#pragma warning restore CA1822 // Mark members as static
