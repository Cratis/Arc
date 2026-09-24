// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>Introduces request-only provider failures after startup validation has completed.</summary>
/// <param name="options">The host's configured policies.</param>
/// <param name="http">The active request.</param>
public class ScenarioAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options, IHttpContextAccessor http) : DefaultAuthorizationPolicyProvider(options)
{
    /// <inheritdoc/>
    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (http.HttpContext?.Request.Headers.ContainsKey("X-Unknown-Runtime-Policy") == true)
        {
            return Task.FromResult<AuthorizationPolicy?>(null);
        }

        if (http.HttpContext?.Request.Headers.ContainsKey("X-Throw-Runtime-Policy") == true)
        {
            throw new ScenarioPolicyProviderFailure();
        }

        return base.GetPolicyAsync(policyName);
    }
}

/// <summary>The exception that is thrown when a scenario policy provider is configured to fail.</summary>
public class ScenarioPolicyProviderFailure : Exception
{
    /// <summary>Initializes the simulated provider failure.</summary>
    public ScenarioPolicyProviderFailure() : base("SensitivePolicyProviderType and scheme Special are unavailable")
    {
    }
}
