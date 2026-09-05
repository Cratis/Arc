// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Verification;

/// <summary>
/// Prints a document, compiles the printed text and prints the result again.
/// </summary>
/// <remarks>
/// Every generation compiles what it printed, because output the language rejects is output nobody can use. This
/// goes one step further and prints the recompiled document a second time, which asks whether anything was lost on
/// the way through the text - a stronger claim, and one that can only be made where a document is small enough to
/// be read and compared. That is why generating uses only the compiling half of this and specifying uses all of it.
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
        var verification = new ScreenplayVerifier().Verify(printer.Print(application));
        var reprinted = verification.Application is null ? string.Empty : printer.Print(verification.Application);

        return new(verification, reprinted);
    }
}
