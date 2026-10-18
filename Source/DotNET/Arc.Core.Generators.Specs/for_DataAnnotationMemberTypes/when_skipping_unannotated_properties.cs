// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Generators.Specs.for_DataAnnotationMemberTypes;

public class when_skipping_unannotated_properties : given.annotated_models
{
    bool _registered;

    void Because()
    {
        Compile(Models);
        _registered = DataAnnotationMembers.TryGet(_assembly.GetType("Annotated.Unannotated")!, out _);
    }

    [Fact] void should_not_register_a_concept_without_annotations() => _registered.ShouldBeFalse();
    [Fact] void should_not_emit_a_reader_for_an_unannotated_non_concept() => _source.Contains(".@Plain", StringComparison.Ordinal).ShouldBeFalse();
}
