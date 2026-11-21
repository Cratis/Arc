// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Delegation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerConsumer;

public class when_delegation_is_refused : given.a_delegated_consumer
{
    void Establish()
    {
        _services.AddScoped(_ => _authority);
        _authority.IsAllowed(Arg.Any<DelegationRequest>(), Arg.Any<CancellationToken>()).Returns(false);
    }

    Task Because() => InvokeConsumer();

    [Fact] void should_refuse() => _result.ShouldBeNull();
    [Fact] void should_not_reach_product_authorization_or_the_operation() => _operationCaller.ShouldBeNull();
}
