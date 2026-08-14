// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.EndToEnd;

/// <summary>
/// The exception that is thrown when a line of an expectations file declares nothing this can check.
/// </summary>
/// <param name="line">The line that could not be read.</param>
public class UnreadableExpectation(string line)
    : Exception($"'{line}' is not an expectation - write 'says <line>', 'once <line>', 'never <line>' or 'reports <code> <count>'");
