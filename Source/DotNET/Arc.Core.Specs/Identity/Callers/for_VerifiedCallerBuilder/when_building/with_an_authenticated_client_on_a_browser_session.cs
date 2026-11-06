// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerBuilder.when_building;

public class with_an_authenticated_client_on_a_browser_session : given.a_builder
{
    void Establish() => _facts = _facts with { CredentialKind = CallerCredentialKind.BrowserSession, Client = new("browser-client", CallerClientAuthentication.Authenticated) };
    void Because() => Build();
    [Fact] void should_accept() => _result.Refusal.ShouldBeNull();
    [Fact] void should_downgrade_the_client() => _result.Caller!.Client!.Authentication.ShouldEqual(CallerClientAuthentication.Declared);
}
