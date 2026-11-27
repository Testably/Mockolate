using System;
using System.Collections.Generic;
using Mockolate.Tests.GeneratorCoverage;

namespace Mockolate.Tests;

public sealed class MembersDifferingInNullabilityTests
{
	[Test]
	public async Task MethodsDifferingOnlyInNullability_ShouldConfigureAndVerifyEachInterfaceSeparately()
	{
		IMembersDifferingInNullability sut = IMembersDifferingInNullability.CreateMock();
		List<string?> received = [];
		sut.Mock.Setup.Query(It.IsAny<string>()).Do(value => received.Add($"non-nullable {value}"));
		sut.Mock.As<INullableQuery>().Setup.Query(It.IsAny<string?>()).Do(value => received.Add($"nullable {value}"));

		((INonNullableQuery)sut).Query("a");
		((INullableQuery)sut).Query("b");
		((INullableQuery)sut).Query(null);

		await That(received).IsEqualTo(["non-nullable a", "nullable b", "nullable ",]);
		await That(sut.Mock.Verify.Query("a")).Once();
		await That(sut.Mock.Verify.Query(It.IsAny<string>())).Once()
			.Because("the calls through INullableQuery are recorded for its own member");
		await That(sut.Mock.As<INonNullableQuery>().Verify.Query("a")).Once();
		await That(sut.Mock.As<INullableQuery>().Verify.Query(It.IsNull<string?>())).Once();
		await That(sut.Mock.As<INullableQuery>().Verify.Query(It.IsAny<string?>())).Twice();
	}

	[Test]
	public async Task MethodsDifferingInNestedNullabilityAndNames_ShouldConfigureAndVerifyEachInterfaceSeparately()
	{
		IMembersDifferingInNullability sut = IMembersDifferingInNullability.CreateMock();
		List<string> received = [];
		sut.Mock.Setup.Filter(It.IsAny<List<string>>()).Do(_ => received.Add("non-nullable filter"));
		sut.Mock.As<INullableQuery>().Setup.Filter(It.IsAny<List<string?>>()).Do(_ => received.Add("nullable filter"));
		sut.Mock.Setup.Range(It.IsAny<(int, int)>()).Do(_ => received.Add("non-nullable range"));
		sut.Mock.As<INullableQuery>().Setup.Range(It.IsAny<(int, int)>()).Do(_ => received.Add("nullable range"));

		((INonNullableQuery)sut).Filter(["a",]);
		((INullableQuery)sut).Filter([null,]);
		((INonNullableQuery)sut).Range((1, 2));
		((INullableQuery)sut).Range((3, 4));

		await That(received).IsEqualTo(["non-nullable filter", "nullable filter", "non-nullable range", "nullable range",]);
		await That(sut.Mock.Verify.Filter(It.IsAny<List<string>>())).Once();
		await That(sut.Mock.As<INullableQuery>().Verify.Filter(It.IsAny<List<string?>>())).Once();
		await That(sut.Mock.Verify.Range((1, 2))).Once();
		await That(sut.Mock.As<INullableQuery>().Verify.Range((3, 4))).Once();
	}

	[Test]
	public async Task PropertiesDifferingOnlyInNullability_ShouldConfigureAndVerifyEachInterfaceSeparately()
	{
		IMembersDifferingInNullability sut = IMembersDifferingInNullability.CreateMock();
		sut.Mock.Setup.Name.InitializeWith("non-nullable");
		sut.Mock.As<INullableQuery>().Setup.Name.InitializeWith(null);

		string nonNullable = ((INonNullableQuery)sut).Name;
		string? nullable = ((INullableQuery)sut).Name;

		await That(nonNullable).IsEqualTo("non-nullable");
		await That(nullable).IsNull();
		await That(sut.Mock.Verify.Name.Got()).Once();
		await That(sut.Mock.As<INullableQuery>().Verify.Name.Got()).Once();
	}

	[Test]
	public async Task EventsDifferingOnlyInNullability_ShouldRaiseAndVerifyEachInterfaceSeparately()
	{
		IMembersDifferingInNullability sut = IMembersDifferingInNullability.CreateMock();
		List<string> received = [];
		((INonNullableQuery)sut).Changed += (_, _) => received.Add("non-nullable");
		((INullableQuery)sut).Changed += (_, _) => received.Add("nullable");

		sut.Mock.Raise.Changed(sut, EventArgs.Empty);
		sut.Mock.As<INullableQuery>().Raise.Changed(sut, EventArgs.Empty);

		await That(received).IsEqualTo(["non-nullable", "nullable",]);
		await That(sut.Mock.Verify.Changed.Subscribed()).Once();
		await That(sut.Mock.As<INullableQuery>().Verify.Changed.Subscribed()).Once();
	}

	[Test]
	public async Task DiamondHidingWithNullability_ShouldConfigureAndVerifyEachInterfaceSeparately()
	{
		IHidingQueries sut = IHidingQueries.CreateMock();
		List<string?> received = [];
		sut.Mock.Setup.Query(It.IsAny<string>()).Do(value => received.Add($"left {value}"));
		sut.Mock.As<IHidingQueryBase>().Setup.Query(It.IsAny<string>()).Do(value => received.Add($"base {value}"));

		((IHidingQueryLeft)sut).Query("a");
		((IHidingQueryRight)sut).Query(null);
		((IHidingQueryBase)sut).Query("c");

		await That(received).IsEqualTo(["left a", "base c",])
			.Because("the call through IHidingQueryRight has its own slot without a setup");
		await That(sut.Mock.Verify.Query("a")).Once();
		await That(sut.Mock.Verify.Query(It.IsAny<string>())).Once();
		await That(sut.Mock.As<IHidingQueryBase>().Verify.Query("c")).Once();
	}

	[Test]
	public async Task GenericMethodsDifferingOnlyInNullability_ShouldConfigureAndVerifyEachInterfaceSeparately()
	{
		IGenericMembersDifferingInNullability sut = IGenericMembersDifferingInNullability.CreateMock();
		List<string?> received = [];
		sut.Mock.Setup.Accept<int>(It.IsAny<int>()).Do(value => received.Add($"int {value}"));
		sut.Mock.As<IAcceptConstrained<string>>().Setup.Accept<string>(It.IsAny<string?>())
			.Do(value => received.Add($"string {value}"));

		((IAcceptConstrained<int>)sut).Accept(1);
		((IAcceptConstrained<string>)sut).Accept("a");
		((IAcceptConstrained<string>)sut).Accept<string>(null);

		await That(received).IsEqualTo(["int 1", "string a", "string ",]);
		await That(sut.Mock.Verify.Accept<int>(1)).Once();
		await That(sut.Mock.As<IAcceptConstrained<string>>().Verify.Accept<string>("a")).Once();
		await That(sut.Mock.As<IAcceptConstrained<string>>().Verify.Accept<string>(It.IsAny<string?>())).Twice();
	}
}
