// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_XmlDocumentation.when_getting_documentation;

/// <summary>
/// A self-closing documentation element carries its payload in an attribute, so flattening the XML to its text
/// content erases it and leaves the two spaces that surrounded it fused together. The result is worse than a
/// missing sentence: it reads as a complete one that has quietly lost its subject, in an artifact nobody diffs.
/// </summary>
public class for_a_type_with_cross_references : Specification
{
    string? _documentation;

    void Because() => _documentation = typeof(SampleTypeWithCrossReferences).GetDocumentation();

    [Fact] void should_name_the_referenced_type() => _documentation.ShouldContain("{@link SampleTypeWithDocumentation}");
    [Fact] void should_keep_inline_code_as_code() => _documentation.ShouldContain("`gadget`");
    [Fact] void should_not_leave_the_gap_behind() => _documentation.ShouldNotContain("  ");
    [Fact] void should_read_as_the_author_wrote_it() => _documentation.ShouldEqual("A {@link SampleTypeWithDocumentation} and a `gadget`.");
}
