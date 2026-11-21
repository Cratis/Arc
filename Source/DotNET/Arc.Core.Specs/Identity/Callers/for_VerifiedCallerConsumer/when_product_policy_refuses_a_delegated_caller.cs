// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerConsumer;

public class when_product_policy_refuses_a_delegated_caller : given.a_delegated_consumer
{
    void Establish()
    {
        _services.AddScoped(_ => _authority);
        _productAllows = false;
    }

    Task Because() => InvokeConsumer();

    [Fact] void should_honor_product_refusal_despite_delegation_allow() => _result.ShouldEqual("product-refused");
    [Fact] void should_not_execute_the_operation() => _operationCaller.ShouldBeNull();
}
