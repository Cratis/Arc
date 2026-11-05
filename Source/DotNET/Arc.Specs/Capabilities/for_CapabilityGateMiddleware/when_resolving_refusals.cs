// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Capabilities.for_CapabilityGateMiddleware;

public class when_resolving_refusals : given.a_capability_gate
{
    [Theory]
    [InlineData(CallerRefusal.NoCredential, 401)]
    [InlineData(CallerRefusal.NoMatchingSource, 401)]
    [InlineData(CallerRefusal.SourceRefused, 401)]
    [InlineData(CallerRefusal.ResourceMismatch, 401)]
    [InlineData(CallerRefusal.GrantMissing, 401)]
    [InlineData(CallerRefusal.TenantMismatch, 401)]
    [InlineData(CallerRefusal.UnreadableCredentialHeaders, 401)]
    [InlineData(CallerRefusal.CredentialInUri, 400)]
    [InlineData(CallerRefusal.MultipleCredentials, 400)]
    [InlineData(CallerRefusal.MalformedAuthorization, 400)]
    public async Task should_return_only_the_mapped_status(int refusal, int status)
    {
        _verified.GetCurrent().Returns(new CallerResolution(null, (CallerRefusal)refusal));
        await Invoke();
        _context.Response.StatusCode.ShouldEqual(status);
        _nextRan.ShouldBeFalse();
        _callers.Current.ShouldBeNull();
        _context.Response.Headers.WWWAuthenticate.ToString().ShouldEqual(status == 401 ? "Bearer" : string.Empty);
    }
}
