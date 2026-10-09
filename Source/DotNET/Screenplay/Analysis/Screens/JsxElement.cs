// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Represents a JSX element read out of a component.
/// </summary>
/// <param name="Tag">The tag the element is written with.</param>
/// <param name="Position">Where in the text the element starts.</param>
/// <param name="Attributes">The attributes written on it, by name.</param>
/// <param name="Children">The text between its opening and closing tags, empty when it closes itself.</param>
/// <param name="IsReadable">Whether every attribute could be read - false when one is spread in from an object, or the element is not written in a shape the reader follows.</param>
public record JsxElement(
    string Tag,
    int Position,
    IReadOnlyDictionary<string, JsxValue> Attributes,
    string Children,
    bool IsReadable)
{
    /// <summary>
    /// Gets the value of an attribute, when the element states it.
    /// </summary>
    /// <param name="name">The name of the attribute.</param>
    /// <returns>The <see cref="JsxValue"/>, or null when the element does not state it.</returns>
    public JsxValue? this[string name] => Attributes.TryGetValue(name, out var value) ? value : null;
}
