// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Names a command response type the same way in every compilation of an application.
/// </summary>
/// <remarks>
/// A scenario lives in another project than the command it issues, so the response type a scenario observes and the
/// one the handler returns are different symbols. Their fully qualified names, without nullability, are the same.
/// </remarks>
public static class ResponseTypes
{
    /// <summary>
    /// Gets the comparable name of a response type.
    /// </summary>
    /// <param name="type">The response type.</param>
    /// <returns>The fully qualified name without nullable annotation.</returns>
    public static string NameOf(ITypeSymbol type) =>
        type.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
}
