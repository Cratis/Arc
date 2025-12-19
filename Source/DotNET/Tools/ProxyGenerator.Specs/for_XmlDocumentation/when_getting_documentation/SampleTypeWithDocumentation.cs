// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_XmlDocumentation.when_getting_documentation;

/// <summary>
/// A sample type for testing XML documentation extraction.
/// </summary>
public class SampleTypeWithDocumentation
{
    /// <summary>
    /// A property with documentation.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A method with documentation.
    /// </summary>
    /// <param name="value">A parameter with documentation.</param>
    /// <returns>A return value.</returns>
    public string GetValue(string value) => value;
}
