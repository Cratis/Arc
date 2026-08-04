// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.ProxyGenerator.for_XmlDocumentation.when_getting_documentation;

/// <summary>
/// A record parameter's documentation is the largest documented surface a model-bound read model has, and it is
/// read by a third code path that flattened the XML the same way the other two did.
/// </summary>
public class for_a_parameter_with_a_cross_reference : Specification
{
    ParameterInfo _parameter;
    string? _documentation;

    void Establish() => _parameter = typeof(SampleTypeWithCrossReferences)
        .GetMethod(nameof(SampleTypeWithCrossReferences.Compare))!
        .GetParameters()[0];

    void Because() => _documentation = _parameter.GetDocumentation();

    [Fact] void should_read_as_the_author_wrote_it() => _documentation.ShouldEqual("The first value, see {@link SampleTypeWithDocumentation}.");
}
