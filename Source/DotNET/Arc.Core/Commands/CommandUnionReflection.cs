// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Cratis.Arc.Commands;

/// <summary>
/// Preserves untrimmed JIT compatibility for unions that generated code cannot name.
/// </summary>
internal static class CommandUnionReflection
{
    /// <summary>
    /// Reads the compiler-generated case constructors in an untrimmed application.
    /// </summary>
    /// <param name="type">The native union type.</param>
    /// <returns>The declared case types.</returns>
    [RequiresUnreferencedCode("Union case constructors must be preserved. Use the Arc source generator when trimming.")]
    [RequiresDynamicCode("Union reflection is only supported by the untrimmed JIT compatibility path.")]
    internal static IReadOnlyList<Type> CaseTypes(Type type) => type.GetConstructors()
        .Where(constructor => constructor.IsDefined(typeof(CompilerGeneratedAttribute), false) && constructor.GetParameters().Length == 1)
        .Select(constructor => constructor.GetParameters()[0].ParameterType)
        .Distinct()
        .ToArray();
}
#endif
