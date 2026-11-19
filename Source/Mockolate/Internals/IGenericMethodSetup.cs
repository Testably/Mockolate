using System.ComponentModel;
using Mockolate.Setup;

namespace Mockolate.Internals;

/// <summary>
///     Marks a method setup that matches the calls of a generic method with any type arguments, so that verifying the
///     setup does not filter the calls by the method name, which contains the type arguments.
/// </summary>
/// <remarks>
///     This type is public only so that generated code can use it; it is not intended to be used directly.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IGenericMethodSetup : IVerifiableMethodSetup;
