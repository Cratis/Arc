// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound.for_MissingArgumentForQuery;

public class when_creating : Specification
{
    MissingArgumentForQuery _exception;

    void Because() => _exception = new MissingArgumentForQuery("myParam", typeof(Guid), "MyNamespace.MyQuery");

    [Fact] void should_expose_parameter_name() => _exception.ParameterName.ShouldEqual("myParam");
    [Fact] void should_include_parameter_name_in_message() => _exception.Message.ShouldContain("myParam");
    [Fact] void should_include_type_name_in_message() => _exception.Message.ShouldContain(nameof(Guid));
    [Fact] void should_include_query_name_in_message() => _exception.Message.ShouldContain("MyNamespace.MyQuery");
}
