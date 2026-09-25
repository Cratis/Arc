// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_ArcAuthorizationPolicyRuntime;

public class when_a_scheme_is_requested : Specification
{
    Exception? _error;

    async Task Because()
    {
        var runtime = new ArcAuthorizationPolicyRuntime([]);
        await using var services = new ServiceCollection().BuildServiceProvider();
        _error = await Catch.Exception(() => runtime.Validate(
            [AuthorizationRequirement.FromAttribute(null, null, "Bearer")],
            services,
            CancellationToken.None));
    }

    [Fact] void should_reject_schemes_on_the_standalone_host() => _error.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();
}
