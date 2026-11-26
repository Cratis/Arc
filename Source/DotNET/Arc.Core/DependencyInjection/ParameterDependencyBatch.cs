// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection;

/// <summary>
/// Tracks one member's arguments in original slots while acquiring dependencies first.
/// </summary>
internal sealed class ParameterDependencyBatch : IDisposable
{
    static readonly AsyncLocal<ParameterDependencyBatch?> _current = new();
    readonly ParameterDependencyBatch? _previous;
    readonly ParameterInfo[] _original;
    readonly HashSet<ParameterInfo> _resolved = [];
    readonly HashSet<string> _sources;

    ParameterDependencyBatch(IServiceProvider services, ParameterInfo[] parameters)
    {
        _original = parameters;
        Parameters = parameters;
        foreach (var ordering in services.GetService<IInstancesOf<ICanResolveParameterDependency>>()?.OfType<IOrderParameterDependencies>() ?? [])
        {
            Parameters = ordering.Order(services, Parameters);
        }
        Arguments = new object?[parameters.Length];
        _sources = parameters.SelectMany(LookUpByKeyProviderDeclarations.For).OfType<LookUpByAttribute>()
            .Select(lookup => lookup.Parameter).OfType<string>().ToHashSet(StringComparer.Ordinal);
        _previous = _current.Value;
        _current.Value = this;
    }

    /// <summary>
    /// Gets the batch active in this binding flow.
    /// </summary>
    internal static ParameterDependencyBatch? Current => _current.Value;

    /// <summary>
    /// Gets the validated dependency-first acquisition order.
    /// </summary>
    internal ParameterInfo[] Parameters { get; }

    /// <summary>
    /// Gets argument values in their original declaration slots.
    /// </summary>
    internal object?[] Arguments { get; }

    /// <inheritdoc/>
    public void Dispose() => _current.Value = _previous;

    /// <summary>
    /// Preflights a member and begins its argument binding frame.
    /// </summary>
    /// <param name="services">The invocation services.</param>
    /// <param name="parameters">The original member parameters.</param>
    /// <returns>A lease restoring the enclosing frame on disposal.</returns>
    internal static ParameterDependencyBatch Begin(IServiceProvider services, ParameterInfo[] parameters) => new(services, parameters);

    /// <summary>
    /// Records one acquired argument in its original slot.
    /// </summary>
    /// <param name="parameter">The acquired parameter.</param>
    /// <param name="value">The acquired value, including absence.</param>
    internal void Set(ParameterInfo parameter, object? value)
    {
        Arguments[Array.IndexOf(_original, parameter)] = value;
        _resolved.Add(parameter);
    }

    /// <summary>
    /// Looks up an earlier acquisition without resolving it again.
    /// </summary>
    /// <param name="parameter">The source parameter.</param>
    /// <param name="value">The acquired value, including absence.</param>
    /// <returns>Whether the source has been acquired in this frame.</returns>
    internal bool TryGet(ParameterInfo parameter, out object? value)
    {
        value = null;
        if (!_resolved.Contains(parameter)) return false;
        value = Arguments[Array.IndexOf(_original, parameter)];
        return true;
    }

    /// <summary>
    /// Determines whether a provided candidate must not replace this argument.
    /// </summary>
    /// <param name="parameter">The argument being bound.</param>
    /// <returns>Whether a chained member requires this explicit lookup or relationship source to be acquired.</returns>
    internal bool RequiresAcquisition(ParameterInfo parameter)
    {
        if (_sources.Count == 0) return false;
        return _sources.Contains(parameter.Name ?? string.Empty) || CommandAttributePolicy.HasExplicitLookup(parameter);
    }
}
