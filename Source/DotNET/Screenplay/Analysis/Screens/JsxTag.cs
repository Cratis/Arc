// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// The boundaries of an opening JSX tag and its matching children and closing tag.
/// </summary>
/// <param name="Name">The tag name, empty for a fragment.</param>
/// <param name="Start">The opening position.</param>
/// <param name="Parent">The index of the enclosing tag, or -1.</param>
internal sealed record JsxTag(string Name, int Start, int Parent)
{
    /// <summary>
    /// Gets or sets the start of the attributes, before any type arguments.
    /// </summary>
    internal int AttributesStart { get; set; }

    /// <summary>
    /// Gets or sets the first position after the opening tag.
    /// </summary>
    internal int OpeningEnd { get; set; }

    /// <summary>
    /// Gets or sets the end of the children.
    /// </summary>
    internal int ChildrenEnd { get; set; }

    /// <summary>
    /// Gets or sets the first position after the element.
    /// </summary>
    internal int End { get; set; }

    /// <summary>
    /// Gets or sets whether the opening tag closes itself.
    /// </summary>
    internal bool SelfClosing { get; set; }

    /// <summary>
    /// Gets or sets whether there is direct non-whitespace text or an expression child.
    /// </summary>
    internal bool HasContent { get; set; }

    /// <summary>
    /// Gets or sets whether all boundaries were found.
    /// </summary>
    internal bool IsComplete { get; set; }

    /// <summary>
    /// Gets whether this is a fragment, including its named forms.
    /// </summary>
    internal bool IsFragment => Name.Length == 0 || IsNamedFragment(Name);

    /// <summary>
    /// Gets whether JSX treats the name as an intrinsic element.
    /// </summary>
    internal bool IsIntrinsic => Name.Length > 0 && IsHtmlName(Name);

    static bool IsNamedFragment(string name) => name == "Fragment" || name == "React.Fragment";

    static bool IsHtmlName(string name) => IsLowercaseName(name) || HasHtmlSeparator(name);

    static bool IsLowercaseName(string name) => char.IsLower(name[0]) && !name.Contains('.', StringComparison.Ordinal);

    static bool HasHtmlSeparator(string name) => name.Contains('-', StringComparison.Ordinal) || name.Contains(':', StringComparison.Ordinal);
}
