// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Queries;

/// <summary>
/// Reads the portable source-file reference for a query with an actual implementation.
/// </summary>
/// <param name="paths">The application's portable source paths, if available.</param>
/// <param name="diagnostics">Where an unreadable implementation reference is reported.</param>
public class QueryImplementationReader(SourcePaths? paths, ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// Reads a block or expression body's source file without inventing one for a declaration without a body.
    /// </summary>
    /// <param name="method">The query method.</param>
    /// <param name="location">Where the query lives.</param>
    /// <returns>The portable file path, or null.</returns>
    public string? Read(IMethodSymbol method, string location)
    {
        var declaration = method.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax())
            .OfType<MethodDeclarationSyntax>().FirstOrDefault(syntax => syntax.Body is not null || syntax.ExpressionBody is not null);
        if (declaration is null)
        {
            return null;
        }

        var path = paths?.Relative(declaration.SyntaxTree.FilePath);
        if (new ScreenplayNaming().ToFilePath(path) is not null)
        {
            return path;
        }

        diagnostics.Information(
            ScreenplayDiagnosticCodes.UnmappableQuery,
            $"The query '{method.Name}' has a body but no portable performer file path, so its implementation reference was left out",
            location);

        return null;
    }
}
