// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Screenplay.Embedded.Generation;

/// <summary>
/// Resolves MSBuild feature properties the same way the Csc task does.
/// </summary>
static class CompilerFeatures
{
    /// <summary>
    /// Parses the compiler features, including the dedicated interceptor namespace properties.
    /// </summary>
    /// <param name="features">The MSBuild feature list.</param>
    /// <param name="interceptorsNamespaces">The enabled interceptor namespaces.</param>
    /// <param name="interceptorsPreviewNamespaces">The legacy interceptor namespaces.</param>
    /// <returns>The effective parse-option features.</returns>
    public static IReadOnlyDictionary<string, string> Parse(string? features, string? interceptorsNamespaces, string? interceptorsPreviewNamespaces)
    {
        var arguments = new List<string>();
        var namespaces = string.Join(';', new[] { interceptorsNamespaces, interceptorsPreviewNamespaces }.Where(_ => !string.IsNullOrEmpty(_)));
        if (namespaces.Length > 0)
        {
            arguments.Add($"/features:InterceptorsNamespaces={namespaces}");
        }

        // Csc adds the dedicated namespace switch first, followed by one switch per MSBuild feature.
        // Let Roslyn resolve values and duplicate keys rather than inventing another feature grammar.
        arguments.AddRange((features ?? string.Empty)
            .Split([';', ',', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(_ => $"/features:{_.Trim()}"));

        return CSharpCommandLineParser.Default.Parse(arguments, Directory.GetCurrentDirectory(), null).ParseOptions.Features;
    }
}
