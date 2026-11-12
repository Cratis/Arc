// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.HttpListener.when_inspecting;

public class with_only_unsigned_identity_headers : given.a_listener
{
    async Task Because() => await Inspect("X-Forwarded-User: privileged-fixture\r\nX-Forwarded-Email: fixture@example.test\r\nX-Tenant: tenant-fixture\r\nX-Workload: trusted-fixture\r\n");

    [Fact] void should_keep_the_unproven_host_fail_closed() => _result.Refusal.ShouldEqual(CallerRefusal.UnreadableCredentialHeaders);
    [Fact] void should_not_select_a_verifier() => _result.Shape.ShouldBeNull();
}
