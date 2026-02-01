// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Templates;

/// <summary>
/// Extension methods for <see cref="ImportStatement"/>.
/// </summary>
public static class ImportStatementExtensions
{
    /// <summary>
    /// Converts the enumerable of <see cref="ImportStatement"/> to an ordered enumerable.
    /// </summary>
    /// <param name="imports">The imports.</param>
    /// <returns>The ordered enumerable.</returns>
    public static IOrderedEnumerable<ImportStatement> ToOrderedImports(this IEnumerable<ImportStatement> imports) =>
        imports.Order(new ImportStatementComparer());

    sealed class ImportStatementComparer : IComparer<ImportStatement>
    {
        public int Compare(ImportStatement? x, ImportStatement? y)
        {
            var moduleLeft = x?.Module;
            var moduleRight = y?.Module;

            if (moduleLeft == null || moduleRight == null)
            {
                return 0;
            }

            int moduleComparison;
            if (!IsPathImport(moduleLeft) && !IsPathImport(moduleRight))
            {
                moduleComparison = string.Compare(moduleLeft, moduleRight, StringComparison.OrdinalIgnoreCase);
            }
            else if (!IsPathImport(moduleLeft))
            {
                moduleComparison = -1;
            }
            else if (!IsPathImport(moduleRight))
            {
                moduleComparison = 1;
            }
            else
            {
                moduleComparison = string.Compare(GetPathImportFilename(moduleLeft), GetPathImportFilename(moduleRight), StringComparison.OrdinalIgnoreCase);
            }

            // If modules are the same, compare by type name for deterministic ordering
            if (moduleComparison == 0)
            {
                return string.Compare(x?.Type, y?.Type, StringComparison.OrdinalIgnoreCase);
            }

            return moduleComparison;
        }

        static bool IsPathImport(string module) => module.StartsWith("./") || module.StartsWith("../");
        static string GetPathImportFilename(string module) => module.Split('/')[^1];
    }
}
