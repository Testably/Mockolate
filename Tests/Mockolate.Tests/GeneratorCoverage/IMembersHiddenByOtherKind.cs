using System;

namespace Mockolate.Tests.GeneratorCoverage;

public interface IMembersHiddenByOtherKind
{
	string ToString();
	void Start(int value);
	int Size { get; }
	event EventHandler? Stop;
	int Level { get; }
	event EventHandler? Reset;
}
