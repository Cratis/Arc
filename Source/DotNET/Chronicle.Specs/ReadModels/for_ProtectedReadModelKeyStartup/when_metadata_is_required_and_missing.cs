// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_ProtectedReadModelKeyStartup;

public class when_metadata_is_required_and_missing : given.a_startup
{
    Exception _failure;

    void Establish() => Configure(typeof(MissingCommand), requireMetadata: true);

    async Task Because() => _failure = await Catch.Exception(() => _startup.StartAsync(CancellationToken.None));

    [Fact] void should_fail_at_startup() => _failure.ShouldBeOfExactType<CommandReadModelKeysMissing>();

    [ReadProtection(ReadProtectionMode.On)]
    sealed class MissingCommand;
}
