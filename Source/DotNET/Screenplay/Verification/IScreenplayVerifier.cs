// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Verification;

/// <summary>
/// Defines a system that reads a printed Screenplay document back to establish that it is a document at all.
/// </summary>
/// <remarks>
/// This is the half of the generator that trusts nothing the other two halves said. It never sees a compilation or
/// a model - only the text and the Screenplay language - which is what makes a document nobody can use a fact
/// rather than an opinion.
/// </remarks>
public interface IScreenplayVerifier
{
    /// <summary>
    /// Compiles a printed Screenplay document and binds it to an executable semantic model.
    /// </summary>
    /// <param name="source">The printed <c>.play</c> text to compile.</param>
    /// <returns>The <see cref="ScreenplayVerification"/>.</returns>
    ScreenplayVerification Verify(string source);

    /// <summary>
    /// Compiles a printed Screenplay document without binding its potentially incomplete application scope.
    /// </summary>
    /// <param name="source">The printed <c>.play</c> text to compile.</param>
    /// <returns>The syntax verification, without semantic binding diagnostics.</returns>
    /// <remarks>
    /// The default implementation preserves existing verifier implementations while using the compiler the
    /// language ships for this additional syntax-only check.
    /// </remarks>
    ScreenplayVerification VerifySyntax(string source) => new ScreenplayVerifier().VerifySyntax(source);
}
