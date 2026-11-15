using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mockolate.Internals;

namespace Mockolate.Internal.Tests.Internals;

public sealed class InternalsSurfaceTests
{
	[Test]
	public async Task PublicMembers_ShouldNotExposeTargetFrameworkDependentLockTypes()
	{
		const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
		                           BindingFlags.DeclaredOnly;
		IEnumerable<string> offenders = typeof(FastMockInteractions).Assembly.GetTypes()
			.Where(type => type.Namespace == "Mockolate.Internals" && (type.IsPublic || type.IsNestedPublic))
			.SelectMany(type => type.GetMembers(flags).SelectMany(member => SignatureTypes(member)
				.Where(IsLockType)
				.Select(_ => $"{type.FullName}.{member.Name}")));

		await That(offenders).IsEmpty()
			.Because("generated code compiled against one target framework of Mockolate may run against another, so the signatures it can reach must not depend on the target framework");
	}

	private static bool IsLockType(Type type)
	{
		while (type.HasElementType)
		{
			type = type.GetElementType()!;
		}

		return type.FullName is "System.Threading.Lock" or "System.Threading.Lock+Scope";
	}

	private static IEnumerable<Type> SignatureTypes(MemberInfo member)
		=> member switch
		{
			FieldInfo field => [field.FieldType,],
			PropertyInfo property => [property.PropertyType,],
			MethodBase method => method.GetParameters().Select(p => p.ParameterType)
				.Concat(method is MethodInfo m ? [m.ReturnType,] : []),
			_ => [],
		};
}
