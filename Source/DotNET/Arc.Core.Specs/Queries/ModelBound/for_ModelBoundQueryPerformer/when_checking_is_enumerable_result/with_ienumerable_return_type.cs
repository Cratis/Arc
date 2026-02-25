// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_checking_is_enumerable_result;
#pragma warning restore CA1822

public class with_ienumerable_return_type : Specification
{
    ModelBoundQueryPerformer _performer;

    void Establish()
    {
        var serviceProviderIsService = Substitute.For<IServiceProviderIsService>();
        var authorizationEvaluator = Substitute.For<IAuthorizationEvaluator>();
        var method = typeof(OrderQueries).GetMethod(nameof(OrderQueries.AllOrders))!;
        _performer = new ModelBoundQueryPerformer(typeof(OrderReadModel), method, serviceProviderIsService, authorizationEvaluator);
    }

    [Fact] void should_be_enumerable_result() => _performer.IsEnumerableResult.ShouldBeTrue();
}
