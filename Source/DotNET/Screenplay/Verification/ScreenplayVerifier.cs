// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.Verification;

/// <summary>
/// Represents an implementation of <see cref="IScreenplayVerifier"/>.
/// </summary>
/// <param name="compiler">The <see cref="IScreenplayCompiler"/> the printed document is read back with.</param>
/// <remarks>
/// The compiler used is the one the language ships, not a second reading of the grammar written here. Anything
/// less would prove that the document satisfies this package rather than that it satisfies Screenplay, which is
/// the only claim worth making.
/// </remarks>
public class ScreenplayVerifier(IScreenplayCompiler compiler) : IScreenplayVerifier
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenplayVerifier"/> class using the compiler the language ships.
    /// </summary>
    public ScreenplayVerifier()
        : this(new ScreenplayCompiler())
    {
    }

    /// <inheritdoc/>
    public ScreenplayVerification Verify(string source)
    {
        var compilation = compiler.Compile(source);

        return new(source, compilation.Value, [.. compilation.Diagnostics]);
    }
}
