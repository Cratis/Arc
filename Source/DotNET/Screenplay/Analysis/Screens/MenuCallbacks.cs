// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Recognizes only direct hook-backed input openers and literal router callbacks.
/// </summary>
/// <param name="source">The scanned module, excluding comments and opaque text from declaration matches.</param>
/// <param name="components">The module's import bindings.</param>
internal sealed partial class MenuCallbacks(TsxSource source, KnownComponents components)
{
    /// <summary>
    /// The lexical binding proof for hook-backed JSX references.
    /// </summary>
    readonly MenuBindingScope _scope = new(source);

    /// <summary>
    /// Gets the visibility binding of the unique React state tuple for this item's setter.
    /// </summary>
    /// <param name="item">The item containing the callback reference.</param>
    /// <returns>The visibility name, or null when not proven.</returns>
    internal string? Visibility(JsxElement item) => StateOf(item)?.Groups["value"].Value;

    /// <summary>
    /// Proves that the dialog's visibility and the item's setter refer to the same component tuple.
    /// </summary>
    /// <param name="item">The opener item.</param>
    /// <param name="dialog">The dialog bound to visibility.</param>
    /// <returns>Whether both lexical references are proven.</returns>
    internal bool SharesVisibility(JsxElement item, JsxElement dialog) =>
        StateOf(item) is { } state && _scope.Binds(state, dialog.Position);

    /// <summary>
    /// Gets the screen passed to a recognized useDialog opener.
    /// </summary>
    /// <param name="item">The item containing the opener callback.</param>
    /// <returns>The component name, or null when unread.</returns>
    internal string? DialogScreen(JsxElement item)
    {
        var opener = Patterns.DialogOpenerRegex().Match(item["command"]!.Written);
        if (!opener.Success)
        {
            return null;
        }

        var tuples = Tuples().Where(_ => _.Groups["show"].Value == opener.Groups["show"].Value).ToList();
        if (tuples.Count != 1)
        {
            return null;
        }

        var tuple = tuples[0];
        if (!IsImported(tuple.Groups["hook"].Value, "useDialog", "@cratis/arc.react/dialogs") || !_scope.Binds(tuple, item.Position))
        {
            return null;
        }

        return JsxValue.Expression(tuple.Groups["argument"].Value).Identifier;
    }

    /// <summary>
    /// Gets a literal route from a recognized useNavigate callback.
    /// </summary>
    /// <param name="callback">The callback as written.</param>
    /// <returns>The route, or null when unread.</returns>
    internal string? Route(string callback)
    {
        var call = Patterns.RouteCallbackRegex().Match(callback);
        if (!call.Success)
        {
            return null;
        }

        var declarations = CodeMatches(Patterns.NavigateBindingRegex()).Where(_ => _.Groups["name"].Value == call.Groups["name"].Value);
        return declarations.Count(IsNavigateHook) == 1 ? JsxValue.Expression(call.Groups["route"].Value).Literal : null;
    }

    /// <summary>
    /// Resolves a sole setter declaration and proves its lexical relationship to the item.
    /// </summary>
    /// <param name="item">The item containing the setter reference.</param>
    /// <returns>The proven tuple, or null.</returns>
    Match? StateOf(JsxElement item)
    {
        var opener = Patterns.StateOpenerRegex().Match(item["command"]!.Written);
        if (!opener.Success)
        {
            return null;
        }

        var states = Tuples().Where(_ => _.Groups["show"].Value == opener.Groups["show"].Value).ToList();
        if (states.Count != 1)
        {
            return null;
        }

        var state = states[0];
        if (!IsStateHook(state.Groups["hook"].Value))
        {
            return null;
        }

        return _scope.Binds(state, item.Position) ? state : null;
    }

    /// <summary>
    /// Determines whether a declaration calls a router hook imported from its owning package.
    /// </summary>
    /// <param name="declaration">The hook declaration.</param>
    /// <returns>Whether its import proves the hook identity.</returns>
    bool IsNavigateHook(Match declaration) => IsImported(declaration.Groups["hook"].Value, "useNavigate", "react-router") ||
        IsImported(declaration.Groups["hook"].Value, "useNavigate", "react-router-dom");

    /// <summary>
    /// Gets code-only tuple declarations.
    /// </summary>
    /// <returns>The declarations.</returns>
    IEnumerable<Match> Tuples() => CodeMatches(Patterns.TupleRegex());

    /// <summary>
    /// Gets matches starting in code rather than comments, strings or JSX text.
    /// </summary>
    /// <param name="pattern">The declaration pattern.</param>
    /// <returns>The code matches.</returns>
    IEnumerable<Match> CodeMatches(Regex pattern) => pattern.Matches(source.Code).Cast<Match>().Where(_ => source.IsCode(_.Index));

    /// <summary>
    /// Checks React's named or namespace state hook identity.
    /// </summary>
    /// <param name="hook">The hook expression.</param>
    /// <returns>Whether React supplies it.</returns>
    bool IsStateHook(string hook)
    {
        if (hook == "useState" && IsImported(hook, "useState", "react"))
        {
            return true;
        }

        return hook == "React.useState" && CodeMatches(Patterns.ReactImportRegex()).Any();
    }

    /// <summary>
    /// Checks the exported identity of a named import.
    /// </summary>
    /// <param name="local">The local name.</param>
    /// <param name="name">The exported name.</param>
    /// <param name="module">The owning package.</param>
    /// <returns>Whether the import matches.</returns>
    bool IsImported(string local, string name, string module)
    {
        if (!components.Imports.TryGetValue(local, out var binding))
        {
            return false;
        }

        return binding.Name == name && binding.Module == module;
    }

    /// <summary>
    /// Holds the compiled callback and declaration patterns.
    /// </summary>
    internal static partial class Patterns
    {
        /// <summary>
        /// Gets the pattern for a sole boolean state setter call.
        /// </summary>
        /// <returns>The pattern.</returns>
        [GeneratedRegex(@"^\(\s*\)\s*=>\s*(?:\{\s*(?<show>[A-Za-z_$][\w$]*)\(\s*true\s*\)\s*;?\s*\}|(?<show>[A-Za-z_$][\w$]*)\(\s*true\s*\))$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
        internal static partial Regex StateOpenerRegex();

        /// <summary>
        /// Gets the pattern for a sole useDialog opener call.
        /// </summary>
        /// <returns>The pattern.</returns>
        [GeneratedRegex(@"^\(\s*\)\s*=>\s*(?:\{\s*(?<show>[A-Za-z_$][\w$]*)\(\s*\)\s*;?\s*\}|(?<show>[A-Za-z_$][\w$]*)\(\s*\))$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
        internal static partial Regex DialogOpenerRegex();

        /// <summary>
        /// Gets the pattern for a hook tuple declaration.
        /// </summary>
        /// <returns>The pattern.</returns>
        [GeneratedRegex(@"\bconst\s+\[\s*(?<value>[A-Za-z_$][\w$]*)\s*,\s*(?<show>[A-Za-z_$][\w$]*)\s*\]\s*=\s*(?<hook>[A-Za-z_$][\w$.]*)(?:\s*<[^<>;]*>)?\s*\(\s*(?<argument>[A-Za-z_$][\w$]*)\s*\)", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
        internal static partial Regex TupleRegex();

        /// <summary>
        /// Gets the pattern for a literal route callback.
        /// </summary>
        /// <returns>The pattern.</returns>
        [GeneratedRegex("""^\(\s*\)\s*=>\s*(?<name>[A-Za-z_$][\w$]*)\(\s*(?<route>['"][^'"\\]*['"])\s*\)$""", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
        internal static partial Regex RouteCallbackRegex();

        /// <summary>
        /// Gets the pattern for a router hook declaration.
        /// </summary>
        /// <returns>The pattern.</returns>
        [GeneratedRegex(@"\bconst\s+(?<name>[A-Za-z_$][\w$]*)\s*=\s*(?<hook>[A-Za-z_$][\w$]*)\(\s*\)", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
        internal static partial Regex NavigateBindingRegex();

        /// <summary>
        /// Gets the pattern for a React namespace or default import.
        /// </summary>
        /// <returns>The pattern.</returns>
        [GeneratedRegex("""\bimport\s+(?:React|\*\s+as\s+React)\s+from\s*['"]react['"]""", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
        internal static partial Regex ReactImportRegex();
    }
}
