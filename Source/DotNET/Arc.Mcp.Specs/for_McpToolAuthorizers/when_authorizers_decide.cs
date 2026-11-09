// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Capabilities;
using Cratis.Arc.Identity.Callers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Mcp.for_McpToolAuthorizers;

public class when_authorizers_decide : Specification
{
    McpToolDescriptor _tool;
    McpCaller _caller;
    bool _none;
    bool _allAllow;
    bool _oneDenies;
    bool _oneThrows;

    void Establish()
    {
        _tool = McpToolDescriptor.Custom("tool", "A tool.", ["app:read"], McpToolAccess.Read);
        var capability = new CapabilityCaller("capabilities", CapabilityProtocol.Mcp, CapabilityPrincipalKind.Person, "person", null, null, "credential", null, null, new HashSet<string> { "app:read" });
        _caller = new McpCaller(capability, null!);
    }

    async Task Because()
    {
        _none = await Allow();
        _allAllow = await Allow(Authorizer(true), Authorizer(true));
        _oneDenies = await Allow(Authorizer(true), Authorizer(false));
        var throwing = Substitute.For<IMcpToolAuthorizer>();
        throwing.IsAllowed(Arg.Any<McpToolAuthorization>()).Returns<ValueTask<bool>>(_ => throw new InvalidOperationException());
        _oneThrows = await Allow(Authorizer(true), throwing);
    }

    [Fact] void should_allow_when_none_is_registered() => _none.ShouldBeTrue();
    [Fact] void should_allow_when_all_allow() => _allAllow.ShouldBeTrue();
    [Fact] void should_deny_when_one_denies() => _oneDenies.ShouldBeFalse();
    [Fact] void should_deny_when_one_throws() => _oneThrows.ShouldBeFalse();

    static IMcpToolAuthorizer Authorizer(bool allows)
    {
        var authorizer = Substitute.For<IMcpToolAuthorizer>();
        authorizer.IsAllowed(Arg.Any<McpToolAuthorization>()).Returns(ValueTask.FromResult(allows));
        return authorizer;
    }

    async Task<bool> Allow(params IMcpToolAuthorizer[] authorizers)
    {
        var services = new ServiceCollection();
        foreach (var authorizer in authorizers)
        {
            services.AddSingleton(authorizer);
        }
        await using var provider = services.BuildServiceProvider();
        return await McpToolAuthorizers.Allow(_tool, _caller, provider, CancellationToken.None);
    }
}
