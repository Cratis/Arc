// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.HttpListener.when_inspecting;

public class with_two_authorization_lines : given.a_listener
{
    async Task Because() => await Inspect("Authorization: Bearer fixture-a\r\nAuthorization: Bearer fixture-b\r\n");

    [Fact] void should_not_claim_duplicate_preservation() => _preservesDuplicates.ShouldBeFalse();
    [Fact] void should_fail_closed() => _result.Refusal.ShouldEqual(CallerRefusal.UnreadableCredentialHeaders);
    [Fact] void should_show_managed_last_value_behavior() => (OperatingSystem.IsWindows() || _observedAuthorization.SequenceEqual(["Bearer fixture-b"])).ShouldBeTrue();
}
