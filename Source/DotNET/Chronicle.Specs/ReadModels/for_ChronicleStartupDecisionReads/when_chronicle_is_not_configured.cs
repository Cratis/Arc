// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleStartupDecisionReads;

public class when_chronicle_is_not_configured : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_skip_without_resolving_a_command_reader(bool registerOptions)
    {
        var client = Substitute.For<IChronicleClient>();
        var services = new ServiceCollection().AddSingleton(client);
        services.AddScoped<IDecisionReads>(_ => throw new InvalidOperationException("Command reader must not be resolved"));
        if (registerOptions) services.AddSingleton<IOptions<ChronicleClientOptions>>(Options.Create(new ChronicleClientOptions { AutoDiscoverAndRegister = false }));
        await using var provider = services.BuildServiceProvider();
        var reader = await new ChronicleStartupDecisionReads(provider).Get(CancellationToken.None);

        reader.ShouldBeNull();
        client.ReceivedCalls().ShouldBeEmpty();
    }
}
