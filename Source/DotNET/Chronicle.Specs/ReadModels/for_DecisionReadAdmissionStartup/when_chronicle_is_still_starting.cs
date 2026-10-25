// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup.given.Declarations;

namespace Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup;

public class when_chronicle_is_still_starting : given.a_startup
{
    void Establish() => Configure(typeof(OnCommand));
    Task Because() => _startup.StartAsync(CancellationToken.None);

    [Fact] void should_not_request_the_event_store_before_application_started() => _startupReads.Requests.ShouldEqual(0);
    [Fact] void should_not_stop_the_host() => _lifetime.DidNotReceive().StopApplication();
}
