// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationConfigurationValidator;

[Collection("UsesCurrentDirectory")]
public class when_hosts_start_with_malformed_policy_or_scheme : Specification
{
    Exception? _whitespacePolicy;
    Exception? _trailingSchemeSeparator;

    async Task Because()
    {
        _whitespacePolicy = await TryStart(typeof(WhitespacePolicyCommand));
        _trailingSchemeSeparator = await TryStart(typeof(TrailingSchemeCommand));
    }

    [Fact] void should_reject_the_whitespace_only_policy_at_startup() => _whitespacePolicy.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();
    [Fact] void should_reject_the_empty_scheme_after_a_trailing_comma_at_startup() => _trailingSchemeSeparator.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();

    static async Task<Exception?> TryStart(Type commandType)
    {
        var handlers = Substitute.For<ICommandHandlerProviders>();
        var handler = Substitute.For<ICommandHandler>();
        handler.CommandType.Returns(commandType);
        handlers.Handlers.Returns([handler]);
        var performers = Substitute.For<IQueryPerformerProviders>();
        performers.Performers.Returns([]);
        var builder = ArcApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.Services.AddSingleton(handlers);
        builder.Services.AddSingleton(performers);
        await using var app = builder.Build();
        return await Catch.Exception(() => app.StartAsync());
    }

    [Authorize(Policy = " ")]
    public record WhitespacePolicyCommand;

    [Authorize(AuthenticationSchemes = "Bearer,")]
    public record TrailingSchemeCommand;
}
