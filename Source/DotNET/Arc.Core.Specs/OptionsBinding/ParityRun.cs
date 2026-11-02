// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// Binds the same configuration with the reflection binder and with <see cref="ArcOptionsBinder"/> and records both outcomes.
/// </summary>
public class ParityRun
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParityRun"/> class.
    /// </summary>
    /// <param name="values">The configuration, keyed under <c>Cratis:Arc</c>.</param>
    /// <param name="prepare">Optional preparation applied to both instances before binding.</param>
    public ParityRun(IDictionary<string, string?> values, Action<ArcOptions>? prepare = null)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build().GetSection("Cratis:Arc");

        var reflection = new ArcOptions();
        prepare?.Invoke(reflection);
        ReflectionFailure = Capture(() => configuration.Bind(reflection));
        ReflectionSnapshot = OptionsSnapshot.Of(reflection);

        var reflectionFree = new ArcOptions();
        prepare?.Invoke(reflectionFree);
        ReflectionFreeFailure = Capture(() => ArcOptionsBinder.Bind(reflectionFree, configuration));
        ReflectionFreeSnapshot = OptionsSnapshot.Of(reflectionFree);
    }

    /// <summary>
    /// Gets the snapshot after the reflection binder ran.
    /// </summary>
    public string ReflectionSnapshot { get; }

    /// <summary>
    /// Gets the snapshot after the reflection-free binder ran.
    /// </summary>
    public string ReflectionFreeSnapshot { get; }

    /// <summary>
    /// Gets the failure of the reflection binder, as type and message with its inner failures, or empty.
    /// </summary>
    public string ReflectionFailure { get; }

    /// <summary>
    /// Gets the failure of the reflection-free binder, as type and message with its inner failures, or empty.
    /// </summary>
    public string ReflectionFreeFailure { get; }

    static string Capture(Action bind)
    {
        try
        {
            bind();
            return string.Empty;
        }
        catch (Exception ex)
        {
            var text = string.Empty;
            for (var current = ex; current is not null; current = current.InnerException)
            {
                text += $"{current.GetType().FullName}: {current.Message}\n";
            }

            return text;
        }
    }
}
