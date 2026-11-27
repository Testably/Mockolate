using System.Threading;
using System.Threading.Tasks;

namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     The shape of MediatR's <c>ISender</c>: two generic <c>Send</c> overloads whose counterparts on
///     <c>SetupGeneric</c> share a signature, setup name and parameter names, so they are offered once and match the
///     calls of both.
/// </summary>
public interface IAnyTypeSender
{
	Task<TResponse> Send<TResponse>(IAnyTypeRequest<TResponse> request, CancellationToken cancellationToken = default);

	Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
		where TRequest : class;
}
