using System;
using System.Collections.Generic;

namespace Mockolate.TestHelpers;

public interface IChocolateShelfBase
{
	IEnumerable<string> Assortment { get; set; }
	IEnumerable<string> Featured { get; }
	string Label { get; set; }
	IEnumerable<string> this[int index] { get; set; }
	event EventHandler Restocked;
}

public interface IChocolateShelf : IChocolateShelfBase
{
	new IList<string> Assortment { get; set; }
	new IList<string> Featured { get; }
	new int Label { get; set; }
	new IList<string> this[int index] { get; set; }
	new event Action Restocked;
}
