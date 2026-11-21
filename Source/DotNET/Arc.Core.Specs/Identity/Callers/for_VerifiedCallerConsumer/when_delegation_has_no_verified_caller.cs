// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerConsumer;

public class when_delegation_has_no_verified_caller : given.a_delegated_consumer
{
    void Establish()
    {
        _services.AddScoped(_ => _authority);
        _accessor.GetCurrent().Returns(new CallerResolution(null, CallerRefusal.SourceRefused));
    }

    Task Because() => InvokeConsumer();

    [Fact] void should_not_reach_the_authority() => _authority.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_reach_product_authorization_or_the_operation() => _operationCaller.ShouldBeNull();
}
