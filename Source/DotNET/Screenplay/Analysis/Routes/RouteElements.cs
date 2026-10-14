// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Screens;

namespace Cratis.Arc.Screenplay.Analysis.Routes;

/// <summary>
/// Resolves a sole imported JSX component to a screen file, without interpreting factories or lazy imports.
/// </summary>
/// <param name="file">The importing file.</param>
/// <param name="imports">Its named import bindings.</param>
/// <param name="screens">The retained screen identities.</param>
internal sealed class RouteElements(string file, IReadOnlyDictionary<string, ScreenImportBinding> imports, IReadOnlyList<RouteScreen> screens)
{
    /// <summary>
    /// Normalizes the optional TSX module suffix.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The path without its extension.</returns>
    internal static string WithoutExtension(string path) => path.Replace('\\', '/').EndsWith(".tsx", StringComparison.OrdinalIgnoreCase)
        ? path.Replace('\\', '/')[..^4] : path.Replace('\\', '/');

    /// <summary>
    /// Resolves the relative module named by a plain JSX element expression.
    /// </summary>
    /// <param name="expression">The element expression.</param>
    /// <returns>The normalized imported file path, or null when computed or unresolved.</returns>
    internal string? File(string expression)
    {
        var source = TsxSource.Scan(expression);
        var tags = source.Tags.Where(_ => _.Parent < 0).ToList();
        if (source.Problem is not null || tags.Count != 1 || !IsSoleElement(source, tags[0]) ||
            !imports.TryGetValue(tags[0].Name, out var binding) || !IsRelative(binding.Module))
        {
            return null;
        }

        return ModulePaths.Resolve(ScreenFiles.DirectoryOf(file), binding.Module) is { } path ? WithoutExtension(path) : null;
    }

    /// <summary>
    /// Matches an imported file to exactly one retained screen.
    /// </summary>
    /// <param name="path">The imported path without its TSX extension.</param>
    /// <returns>The screen, or null when not uniquely declared.</returns>
    internal RouteScreen? Screen(string? path)
    {
        var found = screens.Where(_ => WithoutExtension(_.Path) == path).ToList();
        return found.Count == 1 ? found[0] : null;
    }

    static bool IsRelative(string module) => module.StartsWith("./", StringComparison.Ordinal) || module.StartsWith("../", StringComparison.Ordinal);

    static bool IsSoleElement(TsxSource source, JsxTag tag) => tag.IsComplete &&
        string.IsNullOrWhiteSpace(source.Code[..tag.Start]) && string.IsNullOrWhiteSpace(source.Code[tag.End..]);
}
