namespace Mockolate.Tests.TestHelpers.RefStruct;

/// <summary>
///     A ref struct used for end-to-end generator tests.
/// </summary>
public readonly ref struct Packet(int id, ReadOnlySpan<byte> payload)
{
	public int Id { get; } = id;
	public ReadOnlySpan<byte> Payload { get; } = payload;
}
