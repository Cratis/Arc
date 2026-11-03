// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection.for_XmlDocumentationSummaries.when_getting_summary;

public class with_a_documented_member : Specification
{
    string _file;
    string _result;

    void Establish()
    {
        _file = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xml");
        File.WriteAllText(_file, "<doc><members><member name=\"T:A.B\"><summary>\n  First line\n  second line\n</summary></member><member name=\"T:A.C\" /></members></doc>");
    }

    void Because() => _result = XmlDocumentationSummaries.Get(_file, "T:A.B");

    void Destroy() => File.Delete(_file);

    [Fact] void should_return_the_normalized_summary() => _result.ShouldEqual("First line second line");
}
