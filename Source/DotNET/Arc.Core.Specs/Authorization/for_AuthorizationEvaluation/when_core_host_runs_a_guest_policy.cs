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
public class when_core_host_runs_a_guest_policy : Specification
{
    CommandResult _command;
    QueryResult _query;
    QueryResult _observable;
    CommandResult _rejectedCommand;
    int _commandsBefore;
    int _queriesBefore;
    int _admissionsBefore;

    async Task Because()
    {
        var builder = ArcApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.Services.AddArcAuthorizationPolicy<GuestPermission>("GuestPermission", evaluatesAnonymous: true);
        builder.Services.AddSingleton(Substitute.For<ICommandKeys>());
        var available = Substitute.For<ITypes>();
        available.All.Returns([typeof(GuestCommand), typeof(RejectedGuestCommand), typeof(GuestReadModel), typeof(GuestObservableReadModel)]);
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
                [$"{typeof(GuestReadModel).FullName}.All"] = typeof(GuestReadModel),
                [$"{typeof(GuestObservableReadModel).FullName}.All"] = typeof(GuestObservableReadModel)
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
        _commandsBefore = GuestCommand.Handled;
        _queriesBefore = GuestReadModel.Performed;
        _admissionsBefore = GuestObservableReadModel.Admitted;
        await using var scope = app.Services.CreateAsyncScope();
        using (execution.As(new ClaimsPrincipal(new ClaimsIdentity())))
        {
            _command = await commands.Execute(new GuestCommand(), scope.ServiceProvider);
            _rejectedCommand = await commands.Execute(new RejectedGuestCommand(), scope.ServiceProvider);
            _query = await queries.Perform(
                new FullyQualifiedQueryName($"{typeof(GuestReadModel).FullName}.All"),
                QueryArguments.Empty,
                Paging.NotPaged,
                Sorting.None,
                scope.ServiceProvider);
            _observable = await queries.Perform(
                new FullyQualifiedQueryName($"{typeof(GuestObservableReadModel).FullName}.All"),
                QueryArguments.Empty,
                Paging.NotPaged,
                Sorting.None,
                scope.ServiceProvider);
        }
    }

    [Fact] void should_authorize_the_guest_command() => _command.IsAuthorized.ShouldBeTrue();
    [Fact] void should_not_report_a_command_error() => _command.ExceptionMessages.ShouldBeEmpty();
    [Fact] void should_reject_a_guest_denied_by_the_policy() => _rejectedCommand.IsAuthorized.ShouldBeFalse();
    [Fact] void should_authorize_the_guest_query() => _query.IsAuthorized.ShouldBeTrue();
    [Fact] void should_run_only_the_authorized_command() => GuestCommand.Handled.ShouldEqual(_commandsBefore + 1);
    [Fact] void should_perform_the_guest_query() => GuestReadModel.Performed.ShouldEqual(_queriesBefore + 1);
    [Fact] void should_admit_the_guest_observable_query() => _observable.IsAuthorized.ShouldBeTrue();
    [Fact] void should_run_the_admitted_observable_query() => GuestObservableReadModel.Admitted.ShouldEqual(_admissionsBefore + 1);
}
