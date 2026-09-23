// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Decides the file and module names of generated TypeScript files.
/// </summary>
/// <remarks>
/// <para>
/// A generated file is normally named after the type (or source file) it represents, as in <c>Author.ts</c>. With the
/// proxy suffix it is <c>Author.proxy.ts</c> instead, which tells generated proxies apart from hand-written
/// TypeScript sharing the same folders.
/// </para>
/// <para>
/// Every place that names a generated file or refers to one goes through here - the file written, the relative import
/// specifier another generated file uses to reach it, and the rewrite that points imports at combined source files -
/// so they cannot disagree. <c>index.ts</c> barrels need nothing: they export whatever the generated files on disk are
/// called. A type mapped to an explicit module is not a generated file and is never suffixed.
/// </para>
/// </remarks>
public static class GeneratedFileNames
{
    /// <summary>
    /// The suffix that marks a generated proxy file, placed before the <c>.ts</c> extension.
    /// </summary>
    public const string ProxySuffix = ".proxy";

    /// <summary>
    /// Gets the suffix applied to generated file and module names; empty when no suffix is used.
    /// </summary>
    public static string Suffix { get; private set; } = string.Empty;

    /// <summary>
    /// Sets whether generated files carry the <see cref="ProxySuffix"/>.
    /// </summary>
    /// <param name="useProxySuffix">True to name generated files <c>Name.proxy.ts</c>, false for <c>Name.ts</c>.</param>
    public static void UseProxySuffix(bool useProxySuffix) => Suffix = useProxySuffix ? ProxySuffix : string.Empty;

    /// <summary>
    /// Gets the file name for a generated module.
    /// </summary>
    /// <param name="baseName">The type or source file name the module is named after.</param>
    /// <returns>The file name, including the <c>.ts</c> extension.</returns>
    public static string FileNameFor(string baseName) => $"{ModuleNameFor(baseName)}.ts";

    /// <summary>
    /// Gets the module name another file imports a generated module by.
    /// </summary>
    /// <param name="baseName">The type or source file name the module is named after.</param>
    /// <returns>The module name, without an extension.</returns>
    public static string ModuleNameFor(string baseName) => $"{baseName}{Suffix}";

    /// <summary>
    /// Gets the type or source file name a generated module name was built from.
    /// </summary>
    /// <param name="moduleName">The module name, as it appears as the last segment of an import specifier.</param>
    /// <returns>The base name, with the suffix removed when present.</returns>
    public static string BaseNameOf(string moduleName) =>
        Suffix.Length > 0 && moduleName.EndsWith(Suffix, StringComparison.Ordinal) ? moduleName[..^Suffix.Length] : moduleName;
}
