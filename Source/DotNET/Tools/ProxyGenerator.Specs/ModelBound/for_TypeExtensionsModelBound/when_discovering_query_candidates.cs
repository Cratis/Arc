// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_TypeExtensionsModelBound;

public class when_discovering_query_candidates : Specification
{
    MethodInfo[] _methods;

    void Because() => _methods = typeof(QueryCandidateReadModel).GetQueryMethods().ToArray();

    [Fact] void should_discover_one_query() => _methods.Length.ShouldEqual(1);
    [Fact] void should_discover_the_public_query() => _methods.Single().Name.ShouldEqual(nameof(QueryCandidateReadModel.All));
    [Fact] void should_recognize_a_query_on_the_read_model() => typeof(QueryCandidateReadModel).HasQueryMethods().ShouldBeTrue();
    [Fact] void should_not_recognize_a_read_model_with_only_helpers() => typeof(ReadModelWithOnlyLocalFunction).IsQuery().ShouldBeFalse();
}
