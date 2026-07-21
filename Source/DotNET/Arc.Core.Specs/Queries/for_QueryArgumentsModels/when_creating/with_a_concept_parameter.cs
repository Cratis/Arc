// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryArgumentsModels.when_creating;

/// <summary>
/// Concepts are the framework's preferred way to type an argument, so the argument set has to accept the underlying
/// value and rebuild the concept — otherwise a validator declared for the concept would never see one.
/// </summary>
public class with_a_concept_parameter : given.query_arguments_models
{
    bool _result;
    object _model;

    void Establish() => ForQuery("SearchByTenant", new QueryParameter("tenant", typeof(TenantId)));

    void Because() => _result = _models.TryCreateFor(_performer, ArgumentsOf(("tenant", "acme")), out _model);

    [Fact] void should_resolve_a_model() => _result.ShouldBeTrue();
    [Fact] void should_rebuild_the_concept() => ((SearchByTenantParameters)_model).Tenant.ShouldEqual(new TenantId("acme"));
}
