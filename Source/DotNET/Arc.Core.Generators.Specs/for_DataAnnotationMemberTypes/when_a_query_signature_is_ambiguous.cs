// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Generators.Specs.for_DataAnnotationMemberTypes;

public class when_a_query_signature_is_ambiguous : given.annotated_models
{
    bool _registered;
    Exception? _error;

    void Because() => _error = Catch.Exception(() =>
    {
        Compile(Models.Replace("internal static Report Internal(", "public static object Search<T>(Number count, Text name) => new();\ninternal static Report Internal(", StringComparison.Ordinal));
        _registered = DataAnnotationMembers.TryGet(new FullyQualifiedQueryName(_queryName), out _);
    });

    [Fact] void should_not_register_the_query() => _registered.ShouldBeFalse();
    [Fact] void should_compile_without_generated_diagnostics() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_not_throw_during_initialization() => _error.ShouldBeNull();
}
