// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.ProxyGenerator.for_XmlDocumentation.when_getting_documentation;

public class for_method : Specification
{
    MethodInfo _method;
    string? _documentation;

    void Establish() => _method = typeof(SampleTypeWithDocumentation).GetMethod(nameof(SampleTypeWithDocumentation.GetValue))!;

    void Because() => _documentation = _method.GetDocumentation();

    [Fact] void should_return_documentation() => _documentation.ShouldNotBeNull();
    [Fact] void should_contain_summary() => _documentation!.ShouldContain("A method with documentation");
}
