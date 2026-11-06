// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Identity.Callers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Capabilities.for_CapabilityGateMiddleware;

public class when_a_claims_transformation_adds_roles : given.a_capability_gate
{
    protected override void Configure(IServiceCollection services) => services.AddSingleton<IClaimsTransformation, RoleTransformation>();

    void Establish() => _verified.GetCurrent().Returns(new CallerResolution(_person, null));

    Task Because() => Invoke();

    [Fact] void should_run_handler() => _nextRan.ShouldBeTrue();

    [Fact] void should_apply_the_product_role() => _context.User.IsInRole("editor").ShouldBeTrue();

    [Fact] void should_keep_the_verified_identity() => VerifiedCallerPrincipal.Carries(_context.User, _person).ShouldBeTrue();

    sealed class RoleTransformation : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            var clone = principal.Clone();
            clone.AddIdentity(new ClaimsIdentity([new Claim(ClaimTypes.Role, "editor")]));
            return Task.FromResult(clone);
        }
    }
}
