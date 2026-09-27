// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;

namespace Cratis.Arc.Testing.for_QueryScenario;

public class ChangingPrincipalRuntime : IAuthorizationPolicyRuntime
{
    public Task Validate(IReadOnlyList<AuthorizationRequirement> requirements, IServiceProvider services, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<IAuthorizationPolicyResolution> Resolve(IReadOnlyList<AuthorizationRequirement> requirements, IServiceProvider services, CancellationToken cancellationToken) =>
        Task.FromResult<IAuthorizationPolicyResolution>(new Resolution());

    sealed class Resolution : IAuthorizationPolicyResolution
    {
        public Task<ClaimsPrincipal?> SelectPrincipal(ClaimsPrincipal? principal, IServiceProvider services, CancellationToken cancellationToken) =>
            Task.FromResult<ClaimsPrincipal?>(new ClaimsPrincipal(new ClaimsIdentity([new Claim("permission", "read")], "selected")));

        public Task<bool> IsAuthorized(AuthorizationPolicyContext context, IServiceProvider services, CancellationToken cancellationToken) =>
            Task.FromResult(context.Principal.HasClaim("permission", "read"));
    }
}
