// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_XmlDocumentation.when_getting_documentation;

/// <summary>
/// The property path reads its summary separately from the type path, and used to flatten it separately too. A
/// <c>langword</c> is the shape that shows it: the keyword lives in an attribute, so the sentence loses the very
/// word it was about.
/// </summary>
public class for_a_property_with_a_langword : Specification
{
    string? _documentation;

    void Because() => _documentation = typeof(SampleTypeWithCrossReferences).GetProperty(nameof(SampleTypeWithCrossReferences.Name))!.GetDocumentation();

    [Fact] void should_keep_the_keyword() => _documentation.ShouldContain("`null`");
    [Fact] void should_read_as_the_author_wrote_it() => _documentation.ShouldEqual("Gets or sets a name, which is `null` until set.");
}
