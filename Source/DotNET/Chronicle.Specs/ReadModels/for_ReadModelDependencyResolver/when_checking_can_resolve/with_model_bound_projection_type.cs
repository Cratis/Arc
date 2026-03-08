// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelDependencyResolver.when_checking_can_resolve;

public class with_model_bound_projection_type : given.a_read_model_dependency_resolver
{
    bool _result;

    void Because() => _result = _resolver.CanResolve(typeof(ModelBoundReadModel));

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
