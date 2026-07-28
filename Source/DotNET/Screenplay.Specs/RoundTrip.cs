// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay;

/// <summary>
/// Prints a document, compiles the printed source and prints the result again.
/// </summary>
/// <remarks>
/// This is the correctness gate for everything the generator produces. Output that does not compile means the
/// generator is wrong, and output that does not print identically the second time means information was lost.
/// </remarks>
public static class RoundTrip
{
    /// <summary>
    /// Runs the round trip for a document.
    /// </summary>
    /// <param name="application">The document to round trip.</param>
    /// <returns>The result of the round trip.</returns>
    public static RoundTripResult For(ApplicationSyntax application)
    {
        var printer = new ScreenplayPrinter();
        var compiler = new ScreenplayCompiler();
        var printed = printer.Print(application);
        var compilation = compiler.Compile(printed);
        var reprinted = compilation.Value is null ? string.Empty : printer.Print(compilation.Value);

        return new(printed, reprinted, [.. compilation.Diagnostics]);
    }
}
