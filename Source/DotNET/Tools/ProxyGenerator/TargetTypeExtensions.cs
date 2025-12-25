// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Extension methods for <see cref="TargetType"/>.
/// </summary>
public static class TargetTypeExtensions
{
    /// <summary>
    /// Try to get import statement from target type.
    /// </summary>
    /// <param name="targetType">The target type.</param>
    /// <param name="importStatement">The resulting import statement if it requires so.</param>
    /// <returns>True if it needs an import statement, false if not.</returns>
    public static bool TryGetImportStatement(this TargetType targetType, [NotNullWhen(true)] out ImportStatement? importStatement)
    {
        importStatement = null;
        var requiresImport = !string.IsNullOrEmpty(targetType.Module);
        if (requiresImport)
        {
            importStatement = new ImportStatement(targetType.OriginalType, targetType.Type, targetType.Module);
        }
        return requiresImport;
    }
}
