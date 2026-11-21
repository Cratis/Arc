// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerConsumer;

public class when_delegation_has_no_authority : given.a_delegated_consumer
{
    Task Because() => InvokeConsumer();

    [Fact] void should_refuse() => _result.ShouldBeNull();
    [Fact] void should_not_reach_product_authorization_or_the_operation() => _operationCaller.ShouldBeNull();
}
