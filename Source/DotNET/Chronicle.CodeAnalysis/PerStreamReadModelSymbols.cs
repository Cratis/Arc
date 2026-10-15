// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Recognizes declarations that bind a model to the command's route rather than a lookup convention.
/// </summary>
internal static class PerStreamReadModelSymbols
{
    /// <summary>
    /// Gets the non-inherited Chronicle stream declaration.
    /// </summary>
    /// <param name="model">The model type.</param>
    /// <returns>The stream declaration, if present.</returns>
    internal static AttributeData? Declaration(ITypeSymbol model) => model is INamedTypeSymbol named
        ? EventSourceDefinitionSymbols.Find(named, "Cratis.Chronicle.ReadModels.EventStreamAttribute`1") : null;

    /// <summary>
    /// Gets the model's declared source definition.
    /// </summary>
    /// <param name="declaration">The model's stream declaration.</param>
    /// <returns>The source definition.</returns>
    internal static INamedTypeSymbol Source(AttributeData declaration) => (INamedTypeSymbol)declaration.AttributeClass!.TypeArguments[0];

    /// <summary>
    /// Gets the declared stream name.
    /// </summary>
    /// <param name="declaration">The model's stream declaration.</param>
    /// <returns>The stream name.</returns>
    internal static string Stream(AttributeData declaration) => declaration.ConstructorArguments.FirstOrDefault().Value as string ?? string.Empty;
}
