// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// Compares the outcomes of the linked parity scenarios.
/// </summary>
public static class AssertionStubs
{
    /// <summary>
    /// Checks that two outcomes match.
    /// </summary>
    /// <param name="actual">The actual value.</param>
    /// <param name="expected">The expected value.</param>
    /// <exception cref="InvalidOperationException">The outcomes differ.</exception>
    public static void ShouldEqual(this string actual, string expected)
    {
        if (actual != expected)
        {
            throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
        }
    }
}
