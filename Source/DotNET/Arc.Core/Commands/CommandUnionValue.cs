// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using System.Runtime.CompilerServices;

namespace Cratis.Arc.Commands;

/// <summary>
/// Reads a native union case without allowing a default union to silently succeed.
/// </summary>
internal static class CommandUnionValue
{
    /// <summary>
    /// Gets the non-null active case or rejects an uninitialized union.
    /// </summary>
    /// <param name="value">The returned union.</param>
    /// <returns>The active case.</returns>
    /// <exception cref="InvalidCommandUnionValue">The union has no active non-null case.</exception>
    internal static object Active(IUnion value) => value.Value ?? throw new InvalidCommandUnionValue();
}
#endif
