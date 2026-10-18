// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Generators.Specs.for_DataAnnotationMemberTypes;

public class when_skipping_unannotated_query_parameters : given.annotated_models
{
    bool _registered;

    void Because()
    {
        Compile(Models);
        _registered = DataAnnotationMembers.TryGet(new FullyQualifiedQueryName(_queryName.Replace(".Search", ".Plain", StringComparison.Ordinal)), out _);
    }

    [Fact] void should_compile_without_generated_warnings() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_not_register_parameters() => _registered.ShouldBeFalse();
    [Fact] void should_not_emit_parameter_metadata() => _source.ShouldNotContain("RegisterQueryParameters(\"" + _queryName.Replace(".Search", ".Plain", StringComparison.Ordinal));
}
