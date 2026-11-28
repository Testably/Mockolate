using System;

namespace Mockolate.TestHelpers;

public interface IChocolateTray
{
	string Flavor { get; set; }
	int this[int slot] { get; set; }
	event EventHandler Refilled;
	int Count(string flavor);
}

public interface IChocolateBox
{
	string Flavor { get; set; }
	int this[int slot] { get; set; }
	event EventHandler Refilled;
	int Count(string flavor);
}

public interface IChocolateGiftSet : IChocolateTray, IChocolateBox
{
}
