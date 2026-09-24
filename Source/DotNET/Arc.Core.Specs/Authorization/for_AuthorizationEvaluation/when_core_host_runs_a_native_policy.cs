// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

[Collection("UsesCurrentDirectory")]
public class when_core_host_runs_a_native_policy : Specification
{
    CommandResult _deniedCommand;
    CommandResult _allowedCommand;
    QueryResult _deniedQuery;
    QueryResult _allowedQuery;
    int _commandsBefore;
    int _queriesBefore;

    async Task Because()
    {
        var builder = ArcApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.Services.AddArcAuthorizationPolicy<CorePermission>("CorePermission");
        builder.Services.AddSingleton(Substitute.For<ICommandKeys>());
        var available = Substitute.For<ITypes>();
        available.All.Returns([typeof(CoreProtectedCommand), typeof(CoreProtectedReadModel)]);
        builder.Services.AddSingleton<ICommandHandlerProviders>(_ =>
        {
            var providers = Substitute.For<IInstancesOf<ICommandHandlerProvider>>();
            providers.GetEnumerator().Returns(call => new ICommandHandlerProvider[] { new CommandHandlerProvider(available) }.AsEnumerable().GetEnumerator());
            return new CommandHandlerProviders(providers);
        });
        builder.Services.AddSingleton<IQueryPerformerProviders>(services =>
        {
            var metadata = Substitute.For<IQueryMetadataRegistry>();
            metadata.All.Returns(new Dictionary<string, Type>
            {
                [$"{typeof(CoreProtectedReadModel).FullName}.All"] = typeof(CoreProtectedReadModel)
            });
            var provider = new QueryPerformerProvider(
                available,
                metadata,
                services.GetRequiredService<IServiceProviderIsService>(),
                services.GetRequiredService<IAuthorizationEvaluator>());
            var providers = Substitute.For<IInstancesOf<IQueryPerformerProvider>>();
            providers.GetEnumerator().Returns(call => new IQueryPerformerProvider[] { provider }.AsEnumerable().GetEnumerator());
            return new QueryPerformerProviders(providers);
        });
        await using var app = builder.Build();
        var execution = app.Services.GetRequiredService<ISystemExecution>();
        var commands = app.Services.GetRequiredService<ICommandPipeline>();
        var queries = app.Services.GetRequiredService<IQueryPipeline>();
        _commandsBefore = CoreProtectedCommand.Handled;
        _queriesBefore = CoreProtectedReadModel.Performed;
        await using var scope = app.Services.CreateAsyncScope();
        var queryName = new FullyQualifiedQueryName($"{typeof(CoreProtectedReadModel).FullName}.All");

        using (execution.AsSystem())
        {
            _deniedCommand = await commands.Execute(new CoreProtectedCommand(), scope.ServiceProvider);
            _deniedQuery = await queries.Perform(queryName, QueryArguments.Empty, Paging.NotPaged, Sorting.None, scope.ServiceProvider);
        }

        using (execution.As(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("permission", "core:run"), new Claim(ClaimTypes.Name, "core-caller")], "test"))))
        {
            _allowedCommand = await commands.Execute(new CoreProtectedCommand(), scope.ServiceProvider);
            _allowedQuery = await queries.Perform(queryName, QueryArguments.Empty, Paging.NotPaged, Sorting.None, scope.ServiceProvider);
        }
    }

    [Fact] void should_deny_the_actor_without_the_policy_claim() => _deniedCommand.IsAuthorized.ShouldBeFalse();
    [Fact] void should_deny_the_query_without_the_policy_claim() => _deniedQuery.IsAuthorized.ShouldBeFalse();
    [Fact] void should_authorize_the_command_with_the_claim() => _allowedCommand.IsAuthorized.ShouldBeTrue();
    [Fact] void should_not_have_a_command_error() => _allowedCommand.ExceptionMessages.ShouldBeEmpty();
    [Fact] void should_authorize_the_query_with_the_claim() => _allowedQuery.IsAuthorized.ShouldBeTrue();
    [Fact] void should_handle_only_the_authorized_command() => CoreProtectedCommand.Handled.ShouldEqual(_commandsBefore + 1);
    [Fact] void should_run_only_the_authorized_query() => CoreProtectedReadModel.Performed.ShouldEqual(_queriesBefore + 1);
}
