// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Provides executable counterparts of the metadata-only types the generator walks.
/// </summary>
/// <remarks>
/// <para>
/// Proxy generation inspects the target project through a <see cref="System.Reflection.MetadataLoadContext"/>, whose
/// types carry no executable code. FluentValidation declares its rules imperatively in a validator's constructor, so
/// the only way to read them is to run that constructor - which needs a real, loadable type. This resolves the same
/// type in the default load context, leaving every other part of generation on the metadata types it already uses.
/// </para>
/// <para>
/// Deliberately the default context rather than a private one: a second context would give the process a second copy
/// of every type in the target assembly, and anything that scans loaded assemblies - convention-based dependency
/// injection, most notably - then sees each of them twice.
/// </para>
/// </remarks>
public static class RuntimeValidatorAssemblies
{
    static readonly Dictionary<string, Assembly?> _assembliesByLocation = [];

    /// <summary>
    /// Guards the cache. Deliberately an <see cref="object"/> rather than <c>System.Threading.Lock</c>, which the
    /// net8.0 target this also compiles for does not have.
    /// </summary>
    static readonly object _lock = new();

    /// <summary>
    /// Get the executable counterpart of an assembly.
    /// </summary>
    /// <param name="assembly">The assembly to resolve.</param>
    /// <returns>The loadable assembly, or <see langword="null"/> when it cannot be loaded.</returns>
    public static Assembly? For(Assembly assembly)
    {
        var location = LocationOf(assembly);
        if (location is null)
        {
            return null;
        }

        lock (_lock)
        {
            if (_assembliesByLocation.TryGetValue(location, out var loaded))
            {
                return loaded;
            }

            try
            {
                // Resolves to the already-loaded instance when there is one, so an assembly is never duplicated.
                loaded = Assembly.LoadFrom(location);
            }
            catch (Exception)
            {
                // A project whose dependencies cannot be loaded simply contributes no rules, exactly as before.
                // Failing generation over it would be worse than generating clients that validate less than the
                // server.
                loaded = null;
            }

            _assembliesByLocation[location] = loaded;
            return loaded;
        }
    }

    /// <summary>
    /// Get the executable counterpart of a type.
    /// </summary>
    /// <param name="type">The type to resolve.</param>
    /// <returns>The loadable type, or the type itself when it cannot be resolved.</returns>
    public static Type For(Type type)
    {
        if (type.FullName is null)
        {
            return type;
        }

        try
        {
            return For(type.Assembly)?.GetType(type.FullName) ?? type;
        }
        catch (Exception)
        {
            return type;
        }
    }

    static string? LocationOf(Assembly assembly)
    {
        try
        {
            return string.IsNullOrEmpty(assembly.Location) ? null : assembly.Location;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }
}
