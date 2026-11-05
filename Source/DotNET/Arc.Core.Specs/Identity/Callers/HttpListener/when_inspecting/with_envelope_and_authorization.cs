// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.HttpListener.when_inspecting;

public class with_envelope_and_authorization : given.a_listener
{
    async Task Because() => await Inspect("x-cratis-forwarded-identity-signature: v1.synthetic.signature\r\nAuthorization: Bearer fixture-a\r\n");

    [Fact] void should_refuse_the_unproven_host() => _result.Refusal.ShouldEqual(CallerRefusal.UnreadableCredentialHeaders);
}
