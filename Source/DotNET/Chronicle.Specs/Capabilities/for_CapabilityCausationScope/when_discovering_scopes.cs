// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Capabilities.for_CapabilityCausationScope;

[Collection("UsesCurrentDirectory")]
public class when_discovering_scopes
{
    [Fact]
    public async Task should_discover_one_singleton_scope()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc();
        await using var app = builder.Build();
        var scopes = app.Services.GetRequiredService<IInstancesOf<ICommandExecutionScope>>();
        var capability = scopes.OfType<CapabilityCausationScope>().Single();
        ReferenceEquals(capability, app.Services.GetRequiredService<IInstancesOf<ICommandExecutionScope>>().OfType<CapabilityCausationScope>().Single()).ShouldBeTrue();
    }
}
