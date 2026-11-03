// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.for_ArcBuilderExtensions.when_resolving_startup_decision_reads;

[Collection("UsesCurrentDirectory")]
public class and_automatic_discovery_is_disabled : given.a_cratis_web_host
{
    void Establish() => Build(configureOptions: options =>
    {
        options.EventStore = EventStore;
        options.AutoDiscoverAndRegister = false;
    });

    Task Because() => ResolveStartupReads();

    [Fact] void should_not_check_admission() => _reads.ShouldBeNull();
    [Fact] void should_not_ask_for_an_event_store() => _client.ReceivedCalls().ShouldBeEmpty();
}
