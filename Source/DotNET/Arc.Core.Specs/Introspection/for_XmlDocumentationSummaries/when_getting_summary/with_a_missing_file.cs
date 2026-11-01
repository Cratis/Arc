// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection.for_XmlDocumentationSummaries.when_getting_summary;

public class with_a_missing_file : Specification
{
    string _result;

    void Because() => _result = XmlDocumentationSummaries.Get(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xml"), "T:A.B");

    [Fact] void should_return_empty() => _result.ShouldEqual(string.Empty);
}
