// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Generators.Specs.for_DataAnnotationMemberTypes;

public class when_skipping_types_without_annotated_concepts : given.annotated_models
{
    bool _typeRegistered;
    bool _queryRegistered;

    void Because()
    {
        Compile(Models);
        _typeRegistered = DataAnnotationMembers.TryGet(_assembly.GetType("Annotated.Ordinary")!, out _);
        _queryRegistered = DataAnnotationMembers.TryGet(new FullyQualifiedQueryName(_queryName.Replace(".Search", ".Ordinary", StringComparison.Ordinal)), out _);
    }

    [Fact] void should_leave_the_type_on_the_original_path() => _typeRegistered.ShouldBeFalse();
    [Fact] void should_leave_the_query_on_the_original_path() => _queryRegistered.ShouldBeFalse();
}
