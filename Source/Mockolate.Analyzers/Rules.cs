using Microsoft.CodeAnalysis;

namespace Mockolate.Analyzers;

/// <summary>
///     The rules for mockolate analyzers.
/// </summary>
public static class Rules
{
	private const string UsageCategory = "Usage";

	/// <summary>
	///     Verifications must be used.
	/// </summary>
	public static readonly DiagnosticDescriptor UseVerificationRule =
		CreateDescriptor("Mockolate0001", UsageCategory, DiagnosticSeverity.Error);

	/// <summary>
	///     Mock arguments must be mockable.
	/// </summary>
	public static readonly DiagnosticDescriptor MockabilityRule =
		CreateDescriptor("Mockolate0002", UsageCategory, DiagnosticSeverity.Error);

	// Mockolate0003 (ref-struct-mockability), Mockolate0005 to Mockolate0010 are declared in-place
	// on their analyzers rather than here. The Roslyn ReleaseTrackingAnalyzer needs the descriptor
	// in the same assembly as the analyzer that lists it in SupportedDiagnostics; defining it here
	// as a cross-file static confused it (RS2002).

	/// <summary>
	///     A mocked type must be spelled the same way in the whole compilation.
	/// </summary>
	public static readonly DiagnosticDescriptor SingleSpellingRule =
		CreateDescriptor("Mockolate0004", UsageCategory, DiagnosticSeverity.Error,
			WellKnownDiagnosticTags.CompilationEnd);

	private static DiagnosticDescriptor CreateDescriptor(string diagnosticId, string category,
		DiagnosticSeverity severity, params string[] customTags) => new(
		diagnosticId,
		new LocalizableResourceString(diagnosticId + "Title",
			Resources.ResourceManager, typeof(Resources)),
		new LocalizableResourceString(diagnosticId + "MessageFormat", Resources.ResourceManager,
			typeof(Resources)),
		category,
		severity,
		true,
		new LocalizableResourceString(diagnosticId + "Description", Resources.ResourceManager,
			typeof(Resources)),
		customTags: customTags
	);
}
