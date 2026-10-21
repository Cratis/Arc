// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_ProtectedReadModelKeyStartup;

public class when_a_registered_command_is_preflighted : given.a_startup
{
    Exception _failure;

    void Establish()
    {
        CommandReadModelKeys.RegisterCommand(typeof(MissingCommand));
        Configure(typeof(MissingCommand), requireMetadata: true);
    }

    async Task Because() => _failure = await Catch.Exception(() => _startup.StartAsync(CancellationToken.None));

    [Fact] void should_allow_registered_metadata() => _failure.ShouldBeNull();

    [ReadProtection(ReadProtectionMode.On)]
    sealed class MissingCommand;
}
