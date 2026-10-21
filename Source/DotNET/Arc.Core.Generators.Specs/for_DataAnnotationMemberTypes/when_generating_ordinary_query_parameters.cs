// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.Queries;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Generators.Specs.for_DataAnnotationMemberTypes;

public class when_generating_ordinary_query_parameters : given.annotated_models
{
    DataAnnotationMember _member;

    void Because()
    {
        Compile(Models);
        DataAnnotationMembers.TryGet(new FullyQualifiedQueryName(_queryName.Replace(".Search", ".Ordinary", StringComparison.Ordinal)), out var members);
        _member = members!.Single();
    }

    [Fact] void should_compile_without_generated_warnings() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_register_the_parameter_name() => _member.Name.ShouldEqual("count");
    [Fact] void should_read_the_argument_itself() => _member.ReadValue(50).ShouldEqual(50);
    [Fact] void should_read_the_parameter_attribute() => _member.Attributes().Single().ShouldBeOfExactType<RangeAttribute>();
    [Fact] void should_reject_an_out_of_range_argument() => _member.Attributes().Single().IsValid(_member.ReadValue(5000)).ShouldBeFalse();
}
