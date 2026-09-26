// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_TypeExtensionsModelBound;

public class when_discovering_query_candidates : Specification
{
    MethodInfo[] _methods;

    void Because() => _methods = typeof(QueryCandidateReadModel).GetQueryMethods().ToArray();

    [Fact] void should_discover_three_queries() => _methods.Length.ShouldEqual(3);
    [Fact] void should_discover_the_public_query() => _methods.Any(_ => _.Name == nameof(QueryCandidateReadModel.All)).ShouldBeTrue();
    [Fact] void should_discover_the_internal_query() => _methods.Any(_ => _.Name == "InternalHelper").ShouldBeTrue();
    [Fact] void should_discover_the_derived_collection_query() => _methods.Any(_ => _.Name == nameof(QueryCandidateReadModel.Derived)).ShouldBeTrue();
    [Fact] void should_recognize_a_query_on_the_read_model() => typeof(QueryCandidateReadModel).HasQueryMethods().ShouldBeTrue();
    [Fact] void should_not_recognize_a_read_model_with_only_helpers() => typeof(ReadModelWithOnlyLocalFunction).IsQuery().ShouldBeFalse();
}
