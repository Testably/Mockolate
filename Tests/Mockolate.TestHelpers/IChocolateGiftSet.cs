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

#pragma warning disable S3444 // Interfaces should not simply inherit from base interfaces with colliding members
public interface IChocolateGiftSet : IChocolateTray, IChocolateBox
{
}
#pragma warning restore S3444
