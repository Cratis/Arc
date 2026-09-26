// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

[Collection("UsesCurrentDirectory")]
public class when_core_host_uses_baseline_requirements : Specification
{
    CommandResult _anonymousCommand;
    CommandResult _authenticatedCommand;
    QueryResult _anonymousQuery;
    QueryResult _publicQuery;
    QueryResult _authenticatedQuery;
    QueryResult _explicitQuery;
    bool _fallbackDiscovered;
    int _commandRequirements;

    async Task Because()
    {
        var builder = ArcApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.Services.AddSingleton(Substitute.For<ICommandKeys>());
        var available = Substitute.For<ITypes>();
        available.All.Returns([typeof(BaselineCommand), typeof(BaselineReadModel)]);
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
                [$"{typeof(BaselineReadModel).FullName}.All"] = typeof(BaselineReadModel),
                [$"{typeof(BaselineReadModel).FullName}.Public"] = typeof(BaselineReadModel),
                [$"{typeof(BaselineReadModel).FullName}.Restricted"] = typeof(BaselineReadModel)
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
        _fallbackDiscovered = app.Services.GetRequiredService<IInstancesOf<IFallbackAuthorizationEvaluator>>()
            .Any(evaluator => evaluator is AuthenticationByDefault);
        _commandRequirements = app.Services.GetRequiredService<AuthorizationDeclarations>().For(typeof(BaselineCommand)).Requirements.Count;
        var execution = app.Services.GetRequiredService<ISystemExecution>();
        var commands = app.Services.GetRequiredService<ICommandPipeline>();
        var queries = app.Services.GetRequiredService<IQueryPipeline>();
        await using var scope = app.Services.CreateAsyncScope();
        var all = new FullyQualifiedQueryName($"{typeof(BaselineReadModel).FullName}.All");
        var publicQuery = new FullyQualifiedQueryName($"{typeof(BaselineReadModel).FullName}.Public");
        var restricted = new FullyQualifiedQueryName($"{typeof(BaselineReadModel).FullName}.Restricted");

        using (execution.As(new ClaimsPrincipal(new ClaimsIdentity())))
        {
            _anonymousCommand = await commands.Execute(new BaselineCommand(), scope.ServiceProvider);
            _anonymousQuery = await queries.Perform(all, QueryArguments.Empty, Paging.NotPaged, Sorting.None, scope.ServiceProvider);
            _publicQuery = await queries.Perform(publicQuery, QueryArguments.Empty, Paging.NotPaged, Sorting.None, scope.ServiceProvider);
        }

        using (execution.As(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "member")], "test"))))
        {
            _authenticatedCommand = await commands.Execute(new BaselineCommand(), scope.ServiceProvider);
            _authenticatedQuery = await queries.Perform(all, QueryArguments.Empty, Paging.NotPaged, Sorting.None, scope.ServiceProvider);
            _explicitQuery = await queries.Perform(restricted, QueryArguments.Empty, Paging.NotPaged, Sorting.None, scope.ServiceProvider);
        }
    }

    [Fact] void should_discover_the_fallback_by_convention() => _fallbackDiscovered.ShouldBeTrue();
    [Fact] void should_resolve_the_command_baseline() => _commandRequirements.ShouldBeGreaterThan(0);
    [Fact] void should_deny_an_anonymous_command() => _anonymousCommand.IsAuthorized.ShouldBeFalse();
    [Fact] void should_allow_an_authenticated_command() => _authenticatedCommand.IsAuthorized.ShouldBeTrue();
    [Fact] void should_deny_an_anonymous_query() => _anonymousQuery.IsAuthorized.ShouldBeFalse();
    [Fact] void should_allow_a_method_explicitly_opened_to_anonymous_callers() => _publicQuery.IsAuthorized.ShouldBeTrue();
    [Fact] void should_allow_an_authenticated_query() => _authenticatedQuery.IsAuthorized.ShouldBeTrue();
    [Fact] void should_apply_the_explicit_method_roles_instead_of_the_baseline() => _explicitQuery.IsAuthorized.ShouldBeFalse();

    [Command]
    public record BaselineCommand
    {
        public void Handle() { }
    }

    [ReadModel]
    public record BaselineReadModel(string Value)
    {
        public static BaselineReadModel All() => new("all");

        [AllowAnonymous]
        public static BaselineReadModel Public() => new("public");

        [Authorize(Roles = "Admin")]
        public static BaselineReadModel Restricted() => new("restricted");
    }

    public class AuthenticationByDefault : IFallbackAuthorizationEvaluator
    {
        public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(Type type) =>
            type == typeof(BaselineCommand) || type == typeof(BaselineReadModel) ? [AuthorizationRequirement.FromRoles(null)] : [];

        public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(MethodInfo method) => [];
    }
}
