// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Generators.Specs.for_DataAnnotationMemberTypes;

public class when_resolving_an_internal_query : given.annotated_models
{
    DataAnnotationMember[] _members;

    void Because()
    {
        Compile(Models);
        DataAnnotationMembers.TryGet(new FullyQualifiedQueryName(_queryName.Replace(".Search", ".Internal", StringComparison.Ordinal)), out _members!);
    }

    [Fact] void should_compile_without_generated_warnings() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_register_the_internal_query() => _members.Single().Name.ShouldEqual("count");
    [Fact] void should_resolve_the_attributes() => _members.Single().Attributes().Length.ShouldEqual(1);
}
