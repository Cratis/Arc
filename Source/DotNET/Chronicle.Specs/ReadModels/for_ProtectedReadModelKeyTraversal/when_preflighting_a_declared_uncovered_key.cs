// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.Chronicle.ReadModels.for_ProtectedReadModelKeyTraversal;

public class when_preflighting_a_declared_uncovered_key : given.a_traversal
{
    Exception _error;
    ProtectedReadModelKeyStartup _startup;

    void Establish()
    {
        _options.Strict = true;
        ProtectedReadModelKeyAccessors.DeclareKeyTypes(typeof(ProtectedCommand), typeof(PartialKey));
        var handler = Substitute.For<ICommandHandler>();
        handler.CommandType.Returns(typeof(ProtectedCommand));
        var handlers = Substitute.For<ICommandHandlerProviders>();
        handlers.Handlers.Returns([handler]);
        _startup = new(handlers, new ConfigurationBuilder().Build(), _traversal);
    }

    async Task Because() => _error = await Catch.Exception(() => _startup.StartAsync(CancellationToken.None));
    [Fact] void should_fail_before_accepting_commands() => _error.ShouldBeOfExactType<ProtectedReadModelKeyAccessorMissing>();

    [ReadProtection(ReadProtectionMode.On)]
    sealed class ProtectedCommand;
    sealed class PartialKey;
}
