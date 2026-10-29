// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_handling_refusals;

public class without_a_delivery_identity : given.a_refusal_with_delivery
{
    void Establish() => _context = _context with { Delivery = null };
    async Task Because() => _result = await _executor.Execute([_invocation], _context, _store);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_assign_keys() => _appended[0].IdempotencyKey.ShouldBeNull();
    [Fact] void should_append_the_branch_event() => _appended[0].Event.ShouldEqual(_branch);
}
