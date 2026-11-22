namespace Mockolate.SourceGenerators.Entities;

/// <summary>
///     The <paramref name="Method" /> of a class that implements the adapted member <paramref name="Member" />.
/// </summary>
internal readonly record struct MockAdapterClassMember(string Member, Method Method);
