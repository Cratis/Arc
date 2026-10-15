// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_protecting_reads.with_live_off_siblings;

public class when_the_later_sibling_ends_first : given.two_live_off_siblings
{
    protected override int EndsFirst => 1;

    async Task Because() => _result = await Execute(new OffCommand());

    [Fact] void should_refuse_the_on_command() => _onSibling.ExceptionMessages.Single().ShouldContain("Run nested commands sequentially.");
    [Fact] void should_complete_both_off_siblings() => _offSiblings.All(_ => _.IsSuccess).ShouldBeTrue();
    [Fact] void should_succeed_the_outer_command() => _result.IsSuccess.ShouldBeTrue();
}
