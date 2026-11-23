// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Testing.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_services_are_missing : Specification
{
    CommandAuthorizationResult _result;
    async Task Because()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        _result = await CommandAuthorization.For<given.RoleCommand>(provider, new ClaimsPrincipal());
    }

    [Fact] void should_be_unsupported() => _result.ShouldBeUnsupported();
    [Fact] void should_identify_missing_services() => _result.ReasonCode.ShouldEqual("services-missing");
}
