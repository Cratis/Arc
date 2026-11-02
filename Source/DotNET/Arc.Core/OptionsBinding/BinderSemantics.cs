// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// The control flow of <c>ConfigurationBinder.BindInstance</c>, written once for every bound type. The branches follow the 10.x
/// binder on net10.0 and the 9.x binder, which has no explicit null handling, on net8.0 and net9.0.
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
    internal static void BindInstance(BoundType type, BindingPoint point, IConfiguration config, bool isParentCollection)
    {
        var exists = TryReadValue(config, out var value);
        var hasChildren = config.GetChildren().Any();
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
    /// Reads the value of a section. From Microsoft.Extensions.Configuration.Binder 10 an explicit null, a key a provider holds
    /// without a value, counts as present. Earlier versions ignore a null value.
    /// </summary>
    /// <param name="config">The section.</param>
    /// <param name="value">The value.</param>
    /// <returns>Whether the binder treats the key as present.</returns>
    static bool TryReadValue(IConfiguration config, out string? value)
    {
#if NET10_0_OR_GREATER
        if (config is ConfigurationSection section)
        {
            return section.TryGetValue(null, out value);
        }
#endif
        value = (config as IConfigurationSection)?.Value;
        return value is not null;
    }

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
#if NET10_0_OR_GREATER
        else if (exists)
        {
            point.TrySetValue(point.Value);
        }
#endif
    }
}
