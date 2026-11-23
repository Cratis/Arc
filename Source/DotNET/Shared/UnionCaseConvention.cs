// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc;

/// <summary>
/// Identifies native union cases consistently across compiler symbols and emitted metadata.
/// </summary>
internal static class UnionCaseConvention
{
    /// <summary>
    /// The metadata marker on emitted compiler-generated case constructors.
    /// </summary>
    internal const string CompilerGeneratedAttributeName = "System.Runtime.CompilerServices.CompilerGeneratedAttribute";

    /// <summary>
    /// Reads emitted case types through reflection or MetadataLoadContext without inspecting user constructors.
    /// </summary>
    /// <param name="type">The native union type.</param>
    /// <returns>The compiler-declared case types.</returns>
    internal static IEnumerable<Type> CaseTypes(Type type) => type.GetConstructors()
        .Where(constructor => IsCaseConstructor(
            constructor.GetCustomAttributesData().Any(attribute => attribute.AttributeType.FullName == CompilerGeneratedAttributeName),
            constructor.GetParameters().Length))
        .Select(constructor => constructor.GetParameters()[0].ParameterType)
        .Distinct();

    /// <summary>
    /// Tests the compiler's case-constructor shape, excluding user constructors and the implicit default constructor.
    /// </summary>
    /// <param name="isCompilerGenerated">Whether Roslyn implicitly declared the constructor or metadata marks it compiler-generated.</param>
    /// <param name="parameterCount">The number of constructor parameters.</param>
    /// <returns>Whether the sole parameter declares a union case.</returns>
    /// <remarks>
    /// UnionAttribute has no case list. The compiler emits one generated single-parameter constructor per case;
    /// explicit public single-parameter union constructors are prohibited (CS9374).
    /// </remarks>
    internal static bool IsCaseConstructor(bool isCompilerGenerated, int parameterCount) => isCompilerGenerated && parameterCount == 1;
}
