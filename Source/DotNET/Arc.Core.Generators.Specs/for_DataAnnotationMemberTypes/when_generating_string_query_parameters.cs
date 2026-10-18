// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.Queries;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Generators.Specs.for_DataAnnotationMemberTypes;

public class when_generating_string_query_parameters : given.annotated_models
{
    DataAnnotationMember _member;

    void Because()
    {
        Compile(Models);
        DataAnnotationMembers.TryGet(new FullyQualifiedQueryName(_queryName.Replace(".Search", ".Named", StringComparison.Ordinal)), out var members);
        _member = members!.Single();
    }

    [Fact] void should_compile_without_generated_warnings() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_register_the_parameter_name() => _member.Name.ShouldEqual("name");
    [Fact] void should_use_the_display_name() => _member.DisplayName.ShouldEqual("Customer name");
    [Fact] void should_read_the_argument_itself() => _member.ReadValue("abc").ShouldEqual("abc");
    [Fact] void should_read_all_validation_attributes() => _member.Attributes().Select(attribute => attribute.GetType()).ShouldContainOnly(typeof(RequiredAttribute), typeof(StringLengthAttribute));
}
