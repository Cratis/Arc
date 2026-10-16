// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// The exception that is thrown when a connection template uses a placeholder the request has no value for.
/// </summary>
/// <param name="placeholder">The placeholder that has no value.</param>
/// <param name="available">The placeholders that have values.</param>
public class UnknownConnectionPlaceholder(string placeholder, IEnumerable<string> available)
    : Exception($"The connection template placeholder '{{{placeholder}}}' has no value. Available placeholders are: {string.Join(", ", available.Select(_ => $"{{{_}}}"))}");
