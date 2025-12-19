// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_XmlDocumentation.when_getting_documentation;

public class for_type : Specification
{
    Type _type;
    string? _documentation;

    void Establish() => _type = typeof(SampleTypeWithDocumentation);

    void Because() => _documentation = _type.GetDocumentation();

    [Fact] void should_return_documentation() => _documentation.ShouldNotBeNull();
    [Fact] void should_contain_summary() => _documentation!.ShouldContain("A sample type for testing XML documentation extraction");
}
