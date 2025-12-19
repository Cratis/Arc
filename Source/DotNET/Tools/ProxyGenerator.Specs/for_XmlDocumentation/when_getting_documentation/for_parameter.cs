// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.ProxyGenerator.for_XmlDocumentation.when_getting_documentation;

public class for_parameter : Specification
{
    ParameterInfo _parameter;
    string? _documentation;

    void Establish()
    {
        var method = typeof(SampleTypeWithDocumentation).GetMethod(nameof(SampleTypeWithDocumentation.GetValue))!;
        _parameter = method.GetParameters()[0];
    }

    void Because() => _documentation = _parameter.GetDocumentation();

    [Fact] void should_return_documentation() => _documentation.ShouldNotBeNull();
    [Fact] void should_contain_summary() => _documentation!.ShouldContain("A parameter with documentation");
}
