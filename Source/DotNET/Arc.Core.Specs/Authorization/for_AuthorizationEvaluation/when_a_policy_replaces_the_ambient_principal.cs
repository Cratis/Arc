// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Http;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

[Collection("UsesCurrentDirectory")]
public class when_a_policy_replaces_the_ambient_principal : Specification
{
    CommandResult _defaultCommand;
    QueryResult _defaultQuery;
    CommandResult _optedCommand;
    QueryResult _optedQuery;
    CommandResult _guestCommand;
    QueryResult _guestQuery;
    int _commandsBefore;
    int _queriesBefore;

    async Task Because()
    {
        _commandsBefore = SwitchedCommand.Handled;
        _queriesBefore = SwitchedReadModel.Performed;
        (_defaultCommand, _defaultQuery) = await Run(evaluatesAnonymous: false, guest: false);
        (_optedCommand, _optedQuery) = await Run(evaluatesAnonymous: true, guest: false);
        (_guestCommand, _guestQuery) = await Run(evaluatesAnonymous: true, guest: true);
    }

    [Fact] void should_not_execute_a_command_under_an_unevaluated_actor_without_opt_in() => _defaultCommand.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_perform_a_query_under_an_unevaluated_actor_without_opt_in() => _defaultQuery.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_execute_a_command_under_an_unevaluated_actor_with_opt_in() => _optedCommand.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_perform_a_query_under_an_unevaluated_actor_with_opt_in() => _optedQuery.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_execute_a_guest_command_after_the_ambient_actor_changes() => _guestCommand.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_perform_a_guest_query_after_the_ambient_actor_changes() => _guestQuery.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_invoke_the_command_handler() => SwitchedCommand.Handled.ShouldEqual(_commandsBefore);
    [Fact] void should_not_invoke_the_query_performer() => SwitchedReadModel.Performed.ShouldEqual(_queriesBefore);

    static async Task<(CommandResult Command, QueryResult Query)> Run(bool evaluatesAnonymous, bool guest)
    {
        var builder = ArcApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.Services.AddArcAuthorizationPolicy<SwitchingPolicy>("Switching", evaluatesAnonymous);
        builder.Services.AddSingleton(Substitute.For<ICommandKeys>());
        var available = Substitute.For<ITypes>();
        available.All.Returns([typeof(SwitchedCommand), typeof(SwitchedReadModel)]);
        builder.Services.AddSingleton<ICommandHandlerProviders>(_ =>
        {
            var providers = Substitute.For<IInstancesOf<ICommandHandlerProvider>>();
            providers.GetEnumerator().Returns(_ => new ICommandHandlerProvider[] { new CommandHandlerProvider(available) }.AsEnumerable().GetEnumerator());
            return new CommandHandlerProviders(providers);
        });
        builder.Services.AddSingleton<IQueryPerformerProviders>(services =>
        {
            var metadata = Substitute.For<IQueryMetadataRegistry>();
            metadata.All.Returns(new Dictionary<string, Type>
            {
                [$"{typeof(SwitchedReadModel).FullName}.All"] = typeof(SwitchedReadModel)
            });
            var provider = new QueryPerformerProvider(
                available,
                metadata,
                services.GetRequiredService<IServiceProviderIsService>(),
                services.GetRequiredService<IAuthorizationEvaluator>());
            var providers = Substitute.For<IInstancesOf<IQueryPerformerProvider>>();
            providers.GetEnumerator().Returns(_ => new IQueryPerformerProvider[] { provider }.AsEnumerable().GetEnumerator());
            return new QueryPerformerProviders(providers);
        });
        await using var app = builder.Build();
        var requests = app.Services.GetRequiredService<IHttpRequestContextAccessor>();
        await using var scope = app.Services.CreateAsyncScope();
        var request = Substitute.For<IHttpRequestContext>();
        request.RequestServices.Returns(scope.ServiceProvider);
        var originalPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "evaluated")], guest ? null : "test"));
        request.User = originalPrincipal;
        requests.Current = request;
        try
        {
            var command = await app.Services.GetRequiredService<ICommandPipeline>().Execute(new SwitchedCommand(), scope.ServiceProvider);
            request.User = originalPrincipal;
            var query = await app.Services.GetRequiredService<IQueryPipeline>().Perform(
                new FullyQualifiedQueryName($"{typeof(SwitchedReadModel).FullName}.All"),
                QueryArguments.Empty,
                Paging.NotPaged,
                Sorting.None,
                scope.ServiceProvider);
            return (command, query);
        }
        finally
        {
            requests.Current = null;
        }
    }

    public class SwitchingPolicy(IHttpRequestContextAccessor requests) : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            requests.Current!.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, "unevaluated")], "test"));
            return ValueTask.FromResult(true);
        }
    }

    [Command]
    [Authorize(Policy = "Switching")]
    public record SwitchedCommand
    {
        static int _handled;
        public static int Handled => Volatile.Read(ref _handled);
        public void Handle() => Interlocked.Increment(ref _handled);
    }

    [ReadModel]
    [Authorize(Policy = "Switching")]
    public record SwitchedReadModel(string Value)
    {
        static int _performed;
        public static int Performed => Volatile.Read(ref _performed);
        public static SwitchedReadModel All()
        {
            Interlocked.Increment(ref _performed);
            return new("unexpected");
        }
    }
}
