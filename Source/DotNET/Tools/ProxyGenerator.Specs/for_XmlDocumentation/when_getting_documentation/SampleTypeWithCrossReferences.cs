// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_XmlDocumentation.when_getting_documentation;

/// <summary>
/// A <see cref="SampleTypeWithDocumentation"/> and a <c>gadget</c>.
/// </summary>
/// <remarks>
/// Documented the way the .NET conventions ask for, which is the point: the more idiomatic the source, the more
/// there is to lose. Every element here carries its payload in an attribute rather than in a text child, so
/// flattening the XML to its text content erases all of them and fuses the prose around the hole.
/// </remarks>
public class SampleTypeWithCrossReferences
{
    /// <summary>
    /// Gets or sets a name, which is <see langword="null"/> until set.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Compares <paramref name="first"/> against <paramref name="second"/>.
    /// </summary>
    /// <param name="first">The first value, see <see cref="SampleTypeWithDocumentation"/>.</param>
    /// <param name="second">The second value.</param>
    /// <returns>A value.</returns>
    public string Compare(string first, string second) => first + second;

    /// <summary>
    /// Refers to a <see cref="SampleTypeWithDocumentation">sample type</see> by an explicit label.
    /// </summary>
    /// <returns>A value.</returns>
    public string WithLabel() => string.Empty;
}
