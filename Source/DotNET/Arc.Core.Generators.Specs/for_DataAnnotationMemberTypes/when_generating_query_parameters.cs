// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.Queries;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Generators.Specs.for_DataAnnotationMemberTypes;

public class when_generating_query_parameters : given.annotated_models
{
    DataAnnotationMember[] _members;
    object _number;
    object _text;

    void Because()
    {
        Compile(Models);
        DataAnnotationMembers.TryGet(new FullyQualifiedQueryName(_queryName), out _members!);
        _number = Activator.CreateInstance(_assembly.GetType("Annotated.Number")!, 6)!;
        _text = Activator.CreateInstance(_assembly.GetType("Annotated.Text")!, string.Empty)!;
    }

    [Fact] void should_compile_without_generated_warnings() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_register_the_query_identity() => _members.Select(member => member.Name).ShouldContainOnly("count", "name");
    [Fact] void should_reject_an_out_of_range_value() => _members[0].Attributes().Single().IsValid(_members[0].ReadValue(_number)).ShouldBeFalse();
    [Fact] void should_reject_an_empty_required_value() => _members[1].Attributes().Single().IsValid(_members[1].ReadValue(_text)).ShouldBeFalse();
    [Fact] void should_read_range_from_the_parameter() => _members[0].Attributes().Single().ShouldBeOfExactType<RangeAttribute>();
}
