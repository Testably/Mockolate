using System.Runtime.CompilerServices;
using aweXpect.Customization;

namespace Mockolate.SourceGenerators.Tests.TestHelpers;

internal static class Initializer
{
	[ModuleInitializer]
	internal static void Initialize() => Customize.aweXpect.Global.Formatting().MaximumNumberOfCollectionItems.Set(40);
}
