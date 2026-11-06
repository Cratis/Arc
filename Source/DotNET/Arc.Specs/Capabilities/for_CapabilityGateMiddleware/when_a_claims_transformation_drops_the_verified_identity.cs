// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Identity.Callers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Capabilities.for_CapabilityGateMiddleware;

public class when_a_claims_transformation_drops_the_verified_identity : given.a_capability_gate
{
    protected override void Configure(IServiceCollection services) => services.AddSingleton<IClaimsTransformation, ReplacingTransformation>();

    void Establish() => _verified.GetCurrent().Returns(new CallerResolution(_person, null));

    Task Because() => Invoke();

    [Fact] void should_refuse_as_forbidden() => _context.Response.StatusCode.ShouldEqual(403);

    [Fact] void should_not_run_handler() => _nextRan.ShouldBeFalse();

    [Fact] void should_log_the_ambiguous_principal() => _logged.ShouldContain("Capability caller refused: AmbiguousPrincipal");

    sealed class ReplacingTransformation : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal) =>
            Task.FromResult(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "person")], "replaced")));
    }
}
