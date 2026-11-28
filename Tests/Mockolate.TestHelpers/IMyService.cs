using System;
using System.Collections.Generic;

namespace Mockolate.TestHelpers;

public interface IMyService
{
	string this[int index] { get; set; }
	string this[int index1, int index2] { get; set; }
	string this[int index1, int index2, int index3] { get; set; }
	string? this[int? index1, int? index2, int? index3, int? index4] { get; set; }

	bool SomeFlag { get; set; }
	int SomeReadOnlyValue { get; }

	void DoSomething(int value);
	void DoSomething(int value, bool flag);
	void DoSomething(DateTime? value1, double? value2);
	void DoSomething(int? value1, double value2, string? message);
	bool IsValid(int id);

	string DoSomethingAndReturn(int value);

	void MyMethodWithOutParam(out int value);
	void MyMethodWithRefParam(ref int value);
	bool MyMethodWithParams(int a, params bool[] flags);
	bool MyMethodWithOptionalParameters(int a, int b = 0, string c = "foo", MyFlavor d = MyFlavor.Dark);

	event EventHandler? MyEvent;

	TService GetInstance<TService>();

	TService GetInstance<TService>(string key);

	IEnumerable<TService> GetAllInstances<TService>();
}
