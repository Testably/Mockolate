using System;

namespace Mockolate.TestHelpers;

public interface IScenarioService
{
	int Property { get; set; }

	int this[int p1] { get; set; }
	int this[int p1, int p2] { get; set; }
	int this[int p1, int p2, int p3] { get; set; }
	int this[int p1, int p2, int p3, int p4] { get; set; }
	int this[int p1, int p2, int p3, int p4, int p5] { get; set; }
	event EventHandler Event;

	int ReturnMethod0();
	int ReturnMethod1(int p1);
	int ReturnMethod2(int p1, int p2);
	int ReturnMethod3(int p1, int p2, int p3);
	int ReturnMethod4(int p1, int p2, int p3, int p4);
	int ReturnMethod5(int p1, int p2, int p3, int p4, int p5);

	void VoidMethod0();
	void VoidMethod1(int p1);
	void VoidMethod2(int p1, int p2);
	void VoidMethod3(int p1, int p2, int p3);
	void VoidMethod4(int p1, int p2, int p3, int p4);
	void VoidMethod5(int p1, int p2, int p3, int p4, int p5);
}
