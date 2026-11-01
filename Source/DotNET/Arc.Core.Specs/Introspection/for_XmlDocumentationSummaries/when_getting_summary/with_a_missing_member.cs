// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection.for_XmlDocumentationSummaries.when_getting_summary;

public class with_a_missing_member : Specification
{
    string _file;
    string _missing;
    string _withoutSummary;

    void Establish()
    {
        _file = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xml");
        File.WriteAllText(_file, "<doc><members><member name=\"T:A.B\"><summary>Hello</summary></member><member name=\"T:A.C\" /></members></doc>");
    }

    void Because()
    {
        _missing = XmlDocumentationSummaries.Get(_file, "T:A.Missing");
        _withoutSummary = XmlDocumentationSummaries.Get(_file, "T:A.C");
    }

    void Destroy() => File.Delete(_file);

    [Fact] void should_return_empty_for_unknown_member() => _missing.ShouldEqual(string.Empty);
    [Fact] void should_return_empty_for_member_without_summary() => _withoutSummary.ShouldEqual(string.Empty);
}
