// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// The control flow of <c>ConfigurationBinder.BindInstance</c>, written once for every bound type. The branches follow the 10.x
/// binder, which is the version every target framework resolves.
/// </summary>
internal static class BinderSemantics
{
    /// <summary>
    /// Binds one configuration section to a binding point.
    /// </summary>
    /// <param name="type">How the declared type is bound.</param>
    /// <param name="point">The binding point.</param>
    /// <param name="config">The configuration section.</param>
    /// <param name="isParentCollection">Whether the point is an element of a collection or dictionary.</param>
    /// <param name="parent">The section that holds <paramref name="config"/>, which tells an explicit null from a missing key.</param>
    internal static void BindInstance(BoundType type, BindingPoint point, IConfiguration config, bool isParentCollection, IConfiguration? parent = null)
    {
        var value = (config as IConfigurationSection)?.Value;
        var hasChildren = config.GetChildren().Any();
        var exists = value is not null || (!hasChildren && IsExplicitNull(config, parent));
        if (exists && type.TryConvert(value, (config as IConfigurationSection)?.Path, out var result, out var error))
        {
            if (error is not null)
            {
                throw error;
            }

            point.TrySetValue(result);
        }
        else if (hasChildren)
        {
            type.BindChildren(point, config);
        }
        else
        {
            BindTail(type, point, value, exists, isParentCollection);
        }
    }

    /// <summary>
    /// Decides whether the failure of one collection element is ignored. The reflection binder swallows every element
    /// failure unless its <c>ErrorOnUnknownConfiguration</c> option is set, which Arc never sets.
    /// </summary>
    /// <param name="exception">The failure.</param>
    /// <returns>Always <see langword="true"/>.</returns>
    internal static bool IgnoresElementFailure(Exception exception) => exception is not null;

    /// <summary>
    /// An explicit null in the configuration is a key that a provider holds without a value. The parent lists it as a child.
    /// </summary>
    /// <param name="config">The section.</param>
    /// <param name="parent">The section that holds it, if known.</param>
    /// <returns>Whether the section is an explicit null.</returns>
    static bool IsExplicitNull(IConfiguration config, IConfiguration? parent) =>
        parent is not null &&
        config is IConfigurationSection section &&
        parent.GetChildren().Any(_ => string.Equals(_.Key, section.Key, StringComparison.OrdinalIgnoreCase));

    static void BindTail(BoundType type, BindingPoint point, string? value, bool exists, bool isParentCollection)
    {
        if (!string.IsNullOrEmpty(value) || point.IsReadOnly || point.Value is not null)
        {
            return;
        }

        if (isParentCollection)
        {
            point.TrySetValue(type.CreateInstance());
        }
        else if (exists)
        {
            point.TrySetValue(point.Value);
        }
    }
}
