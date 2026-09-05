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
    /// Compiles a printed Screenplay document.
    /// </summary>
    /// <param name="source">The printed <c>.play</c> text to compile.</param>
    /// <returns>The <see cref="ScreenplayVerification"/>.</returns>
    ScreenplayVerification Verify(string source);
}
