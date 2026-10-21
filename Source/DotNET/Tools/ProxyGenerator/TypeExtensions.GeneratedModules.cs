// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

public static partial class TypeExtensions
{
    static IReadOnlyDictionary<string, string> _sourceFileMap = new Dictionary<string, string>();

    /// <summary>
    /// Sets source filenames by full type name before constructing descriptor imports.
    /// </summary>
    /// <param name="sourceFileMap">The source file map, or null to use one file per artifact.</param>
    internal static void SetSourceFileMap(IReadOnlyDictionary<string, string>? sourceFileMap) =>
        _sourceFileMap = sourceFileMap ?? new Dictionary<string, string>();

    /// <summary>
    /// Resolves the generated module base name by the referenced type's full name.
    /// </summary>
    /// <param name="type">Type whose module is being resolved.</param>
    /// <param name="sourceFileMap">The source file map for the current output mode.</param>
    /// <param name="fallbackName">Optional artifact name, such as a query method name, used without a source mapping.</param>
    /// <returns>The source filename when mapped, otherwise the artifact or arity-free type name.</returns>
    internal static string GetGeneratedModuleBaseName(this Type type, IReadOnlyDictionary<string, string>? sourceFileMap, string? fallbackName = null) =>
        type.FullName is { } fullName && sourceFileMap is not null && sourceFileMap.TryGetValue(fullName, out var sourceFile)
            ? sourceFile
            : fallbackName ?? StripGenericArity(type.Name);
}
