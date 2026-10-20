// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_ProtectedReadModelKeyStartup;

public class when_compatibility_is_used : given.a_startup
{
    void Establish() => Configure(typeof(UnregisteredCommand), requireMetadata: false);

    async Task Because()
    {
        await _startup.StartAsync(CancellationToken.None);
        await _startup.StartAsync(CancellationToken.None);
    }

    [Fact] void should_warn_once_per_type() => _logger.Entries.ShouldEqual(1);

    [ReadProtection(ReadProtectionMode.On)]
    sealed class UnregisteredCommand;
}
