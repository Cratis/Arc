// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;

/// <summary>
/// The exception that is thrown when the source a spec hands to an analyzer does not compile.
/// </summary>
/// <remarks>
/// An analyzer verified against source the compiler rejects proves nothing: a misspelled type or member
/// simply stops matching, so a spec expecting no diagnostic passes for the wrong reason.
/// </remarks>
/// <param name="errors">The compiler errors the source produced.</param>
public class SnippetDoesNotCompile(IEnumerable<Diagnostic> errors) : Exception(Describe(errors))
{
    static string Describe(IEnumerable<Diagnostic> errors) =>
        "The source given to the analyzer does not compile, so any diagnostic count it produces is meaningless.\nCompiler errors:\n" +
        string.Join('\n', errors.Select(error => $"  {error.Id} at {error.Location.GetLineSpan().StartLinePosition}: {error.GetMessage()}"));
}
