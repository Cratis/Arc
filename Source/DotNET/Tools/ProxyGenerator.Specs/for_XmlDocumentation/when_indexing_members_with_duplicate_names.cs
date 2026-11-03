// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace Cratis.Arc.ProxyGenerator.for_XmlDocumentation;

public class when_indexing_members_with_duplicate_names : Specification
{
    Dictionary<string, XElement> _index;

    void Because() => _index = XmlDocumentation.IndexMembers(XDocument.Parse(
        "<doc><members><member name=\"T:A\"><summary>first</summary></member><member name=\"T:A\"><summary>second</summary></member><member><summary>unnamed</summary></member></members></doc>"));

    [Fact] void should_index_each_name_once() => _index.Count.ShouldEqual(1);
    [Fact] void should_keep_the_first_occurrence() => _index["T:A"].Element("summary")!.Value.ShouldEqual("first");
}
