// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Bounds a signature walk by active symbols, depth, and total work, without conflating sibling contexts.
/// </summary>
/// <param name="maximumDepth">The maximum number of active types.</param>
sealed class ResponseTypeTraversal(int maximumDepth = NativeUnionSymbols.MaximumDepth)
{
    readonly HashSet<ITypeSymbol> _direct = new(SymbolEqualityComparer.Default);
    readonly HashSet<ITypeSymbol> _inherited = new(SymbolEqualityComparer.Default);
    int _remaining = 4096;

    bool AtLimit => _direct.Count + _inherited.Count >= maximumDepth || _remaining == 0;

    /// <summary>
    /// Evaluates a type once on the current path; enumerable results must be materialized by the callback.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="type">The type being expanded.</param>
    /// <param name="inspect">The bounded recursive inspection.</param>
    /// <param name="skipped">The conservative result for a cycle or exhausted bound.</param>
    /// <param name="inheritedContext">Whether routing or optionality is inherited from a containing response.</param>
    /// <returns>The inspection result, or the conservative result.</returns>
    internal TResult Visit<TResult>(ITypeSymbol type, Func<TResult> inspect, TResult skipped, bool inheritedContext = false)
    {
        var active = inheritedContext ? _inherited : _direct;
        if (AtLimit || !active.Add(type))
        {
            return skipped;
        }
        _remaining--;
        try
        {
            return inspect();
        }
        finally
        {
            active.Remove(type);
        }
    }
}
