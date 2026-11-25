// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Delegation.for_DelegationBoundary;

public class when_no_authority_is_registered : given.a_boundary
{
    async Task Because() => _result = await CreateBoundary().IsAllowed(_request);

    [Fact] void should_refuse() => _result.ShouldBeFalse();
}
