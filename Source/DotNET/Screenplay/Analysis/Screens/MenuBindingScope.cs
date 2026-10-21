// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Proves direct bindings in a component body without interpreting nested functions or expression scopes.
/// </summary>
/// <param name="source">The scanned module boundaries.</param>
internal sealed partial class MenuBindingScope(TsxSource source)
{
    /// <summary>
    /// Determines whether a tuple and a JSX reference share a directly returned component body.
    /// </summary>
    /// <param name="tuple">The hook declaration.</param>
    /// <param name="position">The JSX reference position.</param>
    /// <returns>Whether parameter or nested-scope shadowing can be excluded.</returns>
    internal bool Binds(Match tuple, int position)
    {
        var body = BodyAt(position);
        if (body < 0 || BodyAt(tuple.Index) != body)
        {
            return false;
        }

        var declaration = ComponentRegex().Matches(source.Code).Cast<Match>()
            .SingleOrDefault(_ => _.Index + _.Length - 1 == body);
        if (declaration is null || !source.IsCode(declaration.Index))
        {
            return false;
        }

        if (ShadowsTuple(declaration, tuple))
        {
            return false;
        }

        return IsDirectReturn(body, position) && HasUnshadowedHook(tuple, body);
    }

    /// <summary>
    /// Proves that a literal navigation callback uses the hook binding of its directly returned component.
    /// </summary>
    /// <param name="declaration">The navigation hook declaration.</param>
    /// <param name="position">The JSX callback element position.</param>
    /// <returns>Whether the imported hook and navigation variable are unshadowed.</returns>
    internal bool BindsNavigation(Match declaration, int position)
    {
        var body = BodyAt(position);
        var component = ComponentRegex().Matches(source.Code).Cast<Match>()
            .SingleOrDefault(_ => _.Index + _.Length - 1 == body && source.IsCode(_.Index));
        if (body < 0 || BodyAt(declaration.Index) != body || component is null)
        {
            return false;
        }

        var names = new[] { declaration.Groups["name"].Value, declaration.Groups["hook"].Value };
        var shadowed = IdentifierRegex().Matches(component.Groups["parameters"].Value).Cast<Match>().Any(_ => names.Contains(_.Value, StringComparer.Ordinal));
        return !shadowed && IsDirectReturn(body, position) && HasNavigationHook(declaration, body);
    }

    /// <summary>
    /// Finds the narrow supported component function headers.
    /// </summary>
    /// <returns>The function header pattern.</returns>
    [GeneratedRegex(@"(?:\b(?:const|let|var)\s+[A-Za-z_$][\w$]*\s*=\s*\((?<parameters>[^()]*)\)\s*=>|\bfunction\s+[A-Za-z_$][\w$]*\s*\((?<parameters>[^()]*)\))\s*\{", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ComponentRegex();

    /// <summary>
    /// Finds a direct return immediately before a JSX root.
    /// </summary>
    /// <returns>The return pattern.</returns>
    [GeneratedRegex(@"\breturn\s*\(?\s*$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ReturnRegex();

    /// <summary>
    /// Finds identifiers in a component parameter list.
    /// </summary>
    /// <returns>The identifier pattern.</returns>
    [GeneratedRegex(@"[A-Za-z_$][\w$]*", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex IdentifierRegex();

    /// <summary>
    /// Rejects component parameters that could shadow the tuple or its hook.
    /// </summary>
    /// <param name="component">The component header.</param>
    /// <param name="tuple">The tuple declaration.</param>
    /// <returns>Whether a relevant identifier appears in the parameters.</returns>
    static bool ShadowsTuple(Match component, Match tuple)
    {
        var names = new[] { tuple.Groups["value"].Value, tuple.Groups["show"].Value, tuple.Groups["hook"].Value.Split('.')[0] };
        return IdentifierRegex().Matches(component.Groups["parameters"].Value).Cast<Match>().Any(_ => names.Contains(_.Value, StringComparer.Ordinal));
    }

    /// <summary>
    /// Excludes local hook or namespace bindings by admitting only recognized tuple-call references.
    /// </summary>
    /// <param name="tuple">The tuple whose imported hook must remain unshadowed.</param>
    /// <param name="body">The enclosing component body.</param>
    /// <returns>Whether every code reference to the hook root is a recognized tuple call.</returns>
    bool HasUnshadowedHook(Match tuple, int body)
    {
        var hook = tuple.Groups["hook"].Value.Split('.')[0];
        var references = IdentifierRegex().Matches(source.Code).Cast<Match>()
            .Where(_ => _.Value == hook && IsBodyReference(_, body));
        var calls = MenuCallbacks.Patterns.TupleRegex().Matches(source.Code).Cast<Match>()
            .Select(_ => _.Groups["hook"].Index).ToHashSet();

        return references.All(_ => calls.Contains(_.Index));
    }

    bool HasNavigationHook(Match declaration, int body)
    {
        var hook = declaration.Groups["hook"].Value;
        var references = IdentifierRegex().Matches(source.Code).Cast<Match>().Where(_ => _.Value == hook && IsBodyReference(_, body));
        var calls = MenuCallbacks.Patterns.NavigateBindingRegex().Matches(source.Code).Cast<Match>()
            .Where(_ => source.IsCode(_.Index)).Select(_ => _.Groups["hook"].Index).ToHashSet();
        return references.All(_ => calls.Contains(_.Index));
    }

    /// <summary>
    /// Identifies code references within the matched body, including nested declarations.
    /// </summary>
    /// <param name="reference">The identifier reference.</param>
    /// <param name="body">The component body opening.</param>
    /// <returns>Whether the reference can affect hook identity in this body.</returns>
    bool IsBodyReference(Match reference, int body)
    {
        if (reference.Index <= body || reference.Index >= source.CloseOf(body))
        {
            return false;
        }

        return source.IsCode(reference.Index);
    }

    /// <summary>
    /// Finds the innermost matched brace, including JSX expression boundaries.
    /// </summary>
    /// <param name="position">The source reference position.</param>
    /// <returns>The opening offset, or -1 when there is no enclosing brace.</returns>
    int BodyAt(int position) => Enumerable.Range(0, position).LastOrDefault(_ => Encloses(_, position), -1);

    /// <summary>
    /// Checks whether a scanned brace contains a position.
    /// </summary>
    /// <param name="open">The possible opening brace.</param>
    /// <param name="position">The position.</param>
    /// <returns>Whether this is a containing brace boundary.</returns>
    bool Encloses(int open, int position) => source.Code[open] == '{' && source.CloseOf(open) > position;

    /// <summary>
    /// Requires references in the JSX tree immediately returned by the component.
    /// </summary>
    /// <param name="body">The component body opening.</param>
    /// <param name="position">The JSX reference.</param>
    /// <returns>Whether no helper or conditional expression supplies the JSX root.</returns>
    bool IsDirectReturn(int body, int position)
    {
        var tag = source.Tags.Single(_ => _.Start == position);
        while (tag.Parent >= 0)
        {
            tag = source.Tags[tag.Parent];
        }

        if (tag.Start <= body)
        {
            return false;
        }

        var prefix = source.Code[(body + 1)..tag.Start];
        var returned = ReturnRegex().Match(prefix);
        return returned.Success && source.IsCode(body + 1 + returned.Index);
    }
}
