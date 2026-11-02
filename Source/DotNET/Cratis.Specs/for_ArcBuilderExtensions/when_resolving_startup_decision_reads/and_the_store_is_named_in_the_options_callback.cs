// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Cratis.for_ArcBuilderExtensions.when_resolving_startup_decision_reads;

[Collection("UsesCurrentDirectory")]
public class and_the_store_is_named_in_the_options_callback : given.a_cratis_web_host
{
    void Establish() => Build(configureOptions: options => options.EventStore = EventStore);

    Task Because() => ResolveStartupReads();

    [Fact] void should_use_the_configured_event_store() => _client.Received(1).GetEventStore(Arg.Is<EventStoreName>(_ => _.Value == EventStore));
    [Fact] void should_not_use_an_unnamed_event_store() => _client.DidNotReceive().GetEventStore(Arg.Is<EventStoreName>(_ => string.IsNullOrEmpty(_.Value)));
    [Fact] void should_report_no_reads_while_the_kernel_is_unavailable() => _reads.ShouldBeNull();
}
