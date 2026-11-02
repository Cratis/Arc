// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// An <c>IDictionary&lt;string, TValue&gt;</c> that is populated from the child keys of the section.
/// </summary>
/// <typeparam name="TValue">The value type.</typeparam>
internal sealed class DictionaryType<TValue> : BoundType
{
    readonly BoundType _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="DictionaryType{TValue}"/> class.
    /// </summary>
    /// <param name="value">How the value type is bound.</param>
    internal DictionaryType(BoundType value)
        : base(typeof(IDictionary<string, TValue>))
    {
        _value = value;
    }

    /// <inheritdoc/>
    internal override object CreateInstance() =>
        throw new InvalidOperationException(BinderMessages.CannotActivateAbstractOrInterface(ClrType));

    /// <inheritdoc/>
    internal override void BindChildren(BindingPoint point, IConfiguration config)
    {
        if (point.IsReadOnly && point.Value is null)
        {
            return;
        }

        // ConfigurationBinder.BindDictionaryInterface: an existing dictionary is reused, a missing one is created with the
        // default comparer.
        var dictionary = point.Value as IDictionary<string, TValue> ?? new Dictionary<string, TValue>();
        foreach (var child in config.GetChildren())
        {
            BindEntry(dictionary, child);
        }

        if (!point.IsReadOnly)
        {
            point.SetValue(dictionary);
        }
    }

    void BindEntry(IDictionary<string, TValue> dictionary, IConfigurationSection child)
    {
        try
        {
            var key = child.Key;
            var entryPoint = new BindingPoint(() => dictionary.TryGetValue(key, out var existing) ? existing : default(object), false);
            BinderSemantics.BindInstance(_value, entryPoint, child, true);
            if (entryPoint.HasNewValue)
            {
                dictionary[key] = entryPoint.Value is null ? default! : (TValue)entryPoint.Value;
            }
        }
        catch (Exception ex) when (BinderSemantics.IgnoresElementFailure(ex))
        {
            // ConfigurationBinder.BindDictionary ignores a failing entry and continues with the next one.
        }
    }
}
