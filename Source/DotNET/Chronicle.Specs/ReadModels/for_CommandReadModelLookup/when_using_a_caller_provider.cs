// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandReadModelLookup;

public class when_using_a_caller_provider : given.a_caller_lookup
{
    void Because() => Resolve<Command>();

    [Fact] void should_use_the_caller_in_every_mode() => _keys.ShouldEachConformTo(key => key.Value == "selected");
    [Fact] void should_not_log_claim_values() => _logger.Messages.Exists(message => message.Contains("selected", StringComparison.Ordinal)).ShouldBeFalse();
}
