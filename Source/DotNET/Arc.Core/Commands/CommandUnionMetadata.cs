// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;

namespace Cratis.Arc.Commands;

/// <summary>
/// Registers native union declarations and typed active-value accessors emitted by the Arc source generator.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class CommandUnionMetadata
{
    static readonly CommandUnionMetadataRegistry _registry = new(GeneratedMetadataRegistration.EnsureGeneratedMetadataRegistered);

    /// <summary>
    /// Registers metadata for an exact, closed native union type. Repeated registrations are harmless.
    /// </summary>
    /// <param name="unionType">The union declaration.</param>
    /// <param name="caseTypes">The compiler-declared case types, copied on registration.</param>
    /// <param name="activeValue">The typed accessor for the active case.</param>
    public static void Register(Type unionType, Type[] caseTypes, Func<object, object?> activeValue) =>
        _registry.Register(unionType, caseTypes, activeValue);

    /// <summary>
    /// Resolves generated metadata after activating pending project modules.
    /// </summary>
    /// <param name="unionType">The exact union declaration.</param>
    /// <returns>The generated declaration, if registered.</returns>
    internal static Declaration? For(Type unionType) => _registry.For(unionType);

    /// <summary>
    /// Gets all declared alternatives, falling back only in untrimmed JIT mode.
    /// </summary>
    /// <param name="unionType">The union declaration.</param>
    /// <returns>The declared case types.</returns>
    /// <exception cref="MissingCommandUnionMetadata">Metadata is absent and the compatibility path is disabled.</exception>
    internal static IReadOnlyList<Type> CaseTypes(Type unionType)
    {
        if (For(unionType) is { } declaration)
        {
            return declaration.CaseTypes;
        }

#if NET11_0_OR_GREATER
        if (CommandUnionFeatureSwitches.ReflectionIsSupported)
        {
            return CommandUnionReflection.CaseTypes(unionType);
        }
#endif
        throw new MissingCommandUnionMetadata(unionType);
    }

    /// <summary>
    /// Immutable declaration metadata used by boundary checks and active-value dispatch.
    /// </summary>
    /// <param name="CaseTypes">The compiler-declared alternatives.</param>
    /// <param name="ActiveValue">The typed active-value accessor.</param>
    internal sealed record Declaration(IReadOnlyList<Type> CaseTypes, Func<object, object?> ActiveValue);
}
