// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay;

/// <summary>
/// Represents the options a Screenplay document is generated with.
/// </summary>
public record ScreenplayOptions
{
    /// <summary>
    /// The name used for the domain and module when nothing else can be resolved.
    /// </summary>
    public const string DefaultName = "Application";

    /// <summary>
    /// Gets the name of the domain the generated document belongs to.
    /// </summary>
    /// <remarks>
    /// Defaults to the name of the assembly being analyzed.
    /// </remarks>
    public string? Domain { get; init; }

    /// <summary>
    /// Gets the name of the module every discovered feature is placed within.
    /// </summary>
    /// <remarks>
    /// Defaults to the same value as <see cref="Domain"/>.
    /// </remarks>
    public string? Module { get; init; }

    /// <summary>
    /// Gets the number of leading namespace segments to skip when arranging slices into features.
    /// </summary>
    /// <remarks>
    /// Defaults to zero. Regardless of this value, a leading segment matching the module name is always dropped so
    /// that the module is never repeated as a feature.
    /// </remarks>
    public int? SegmentsToSkip { get; init; }

    /// <summary>
    /// Gets a value indicating whether the modules of the document are taken from the outermost namespace segment of
    /// each slice rather than from one name.
    /// </summary>
    /// <remarks>
    /// Resolved rather than configured. It is reached when nothing named a module and no single assembly names the
    /// application, which is how an application written as several projects arrives. The reasoning that leaves the
    /// domain unnamed there holds for the module too: none of the projects is the application, so one module would
    /// carry a name that belongs to nobody and would gather every project under it. The namespaces already say what
    /// the modules are. Naming a module still collapses the document into that one, which is the way to ask for it.
    /// <para>
    /// Once resolved it stays resolved. Emission resolves a second time against the domain of the model, which by
    /// then is known, and without this the module would quietly be filled in from it and the document would come
    /// back as one module after all.
    /// </para>
    /// </remarks>
    public bool ModulesFromNamespaceRoots { get; init; }

    /// <summary>
    /// Resolves the options with every value filled in.
    /// </summary>
    /// <param name="fallbackName">The name to use for the domain when none is configured.</param>
    /// <returns>The resolved options.</returns>
    public ScreenplayOptions WithDefaults(string? fallbackName)
    {
        var domain = Coalesce(Domain, Coalesce(fallbackName, DefaultName));
        var fromNamespaceRoots = ModulesFromNamespaceRoots ||
            (string.IsNullOrWhiteSpace(Module) && string.IsNullOrWhiteSpace(fallbackName));

        return this with
        {
            Domain = domain,
            Module = fromNamespaceRoots ? null : Coalesce(Module, domain),
            SegmentsToSkip = SegmentsToSkip ?? 0,
            ModulesFromNamespaceRoots = fromNamespaceRoots
        };
    }

    /// <summary>
    /// Gets the first of two values that carries content.
    /// </summary>
    /// <param name="value">The preferred value.</param>
    /// <param name="fallback">The value to fall back to.</param>
    /// <returns>The resolved value.</returns>
    static string Coalesce(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;
}
