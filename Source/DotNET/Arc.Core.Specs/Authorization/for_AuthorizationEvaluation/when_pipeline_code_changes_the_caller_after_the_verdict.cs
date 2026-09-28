// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Http;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Arc.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

[Collection("UsesCurrentDirectory")]
public class when_pipeline_code_changes_the_caller_after_the_verdict : Specification
{
    CommandResult _filteredCommand;
    CommandResult _providedCommand;
    CommandResult _validatedCommand;
    QueryResult _filteredQuery;
    QueryResult _memberQuery;
    int _filteredCommandsBefore;
    int _providedCommandsBefore;
    int _validatedCommandsBefore;
    int _guestQueriesBefore;
    int _memberQueriesBefore;
    int _provided;
    int _providedBefore;
    int _policiesEvaluated;

    async Task Because()
    {
        var builder = ArcApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.Services.AddArcAuthorizationPolicy<CallerPolicy>("Guest", evaluatesAnonymous: true);
        builder.Services.AddArcAuthorizationPolicy<CallerPolicy>("Member");
        builder.Services.AddSingleton(Substitute.For<ICommandKeys>());
        var available = Substitute.For<ITypes>();
        available.All.Returns([typeof(FilteredCommand), typeof(ProvidedCommand), typeof(ValidatedCommand), typeof(GuestReadModel), typeof(MemberReadModel)]);
        var validator = Substitute.For<IModelGraphValidator>();
        validator.Validate(Arg.Any<ModelGraphValidationRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var request = call.Arg<ModelGraphValidationRequest>();
            if (request.Instance is ValidatedCommand)
            {
                request.ServiceProvider.GetRequiredService<IHttpRequestContextAccessor>().Current.User.AddIdentity(
                    new ClaimsIdentity([new Claim(ClaimTypes.Name, "unevaluated")], "test"));
            }

            return Task.FromResult<IEnumerable<ValidationResult>>([]);
        });
        builder.Services.AddSingleton(validator);
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
                [$"{typeof(GuestReadModel).FullName}.All"] = typeof(GuestReadModel),
                [$"{typeof(MemberReadModel).FullName}.All"] = typeof(MemberReadModel)
            });
            var provider = new QueryPerformerProvider(
                available, metadata, services.GetRequiredService<IServiceProviderIsService>(), services.GetRequiredService<IAuthorizationEvaluator>());
            var providers = Substitute.For<IInstancesOf<IQueryPerformerProvider>>();
            providers.GetEnumerator().Returns(_ => new IQueryPerformerProvider[] { provider }.AsEnumerable().GetEnumerator());
            return new QueryPerformerProviders(providers);
        });
        builder.Services.AddSingleton<IInstancesOf<ICommandFilter>>(services => new KnownInstancesOf<ICommandFilter>(
        [
            new Commands.Filters.AuthorizationFilter(services.GetRequiredService<IAuthorizationEvaluator>()),
            new MutatingCommandFilter(),
            new Commands.Filters.FluentValidationFilter(services.GetRequiredService<IModelGraphValidator>())
        ]));
        builder.Services.AddSingleton<IInstancesOf<IQueryFilter>>(services => new KnownInstancesOf<IQueryFilter>(
        [
            new Queries.Filters.AuthorizationFilter(services.GetRequiredService<IQueryPerformerProviders>()),
            new MutatingQueryFilter()
        ]));
        await using var app = builder.Build();
        var requests = app.Services.GetRequiredService<IHttpRequestContextAccessor>();
        await using var scope = app.Services.CreateAsyncScope();
        var request = Substitute.For<IHttpRequestContext>();
        request.RequestServices.Returns(scope.ServiceProvider);
        requests.Current = request;
        ProvidedCommand.Request = request;
        _filteredCommandsBefore = FilteredCommand.Handled;
        _providedCommandsBefore = ProvidedCommand.Handled;
        _validatedCommandsBefore = ValidatedCommand.Handled;
        _guestQueriesBefore = GuestReadModel.Performed;
        _memberQueriesBefore = MemberReadModel.Performed;
        _providedBefore = ProvidedCommand.Provided;
        try
        {
            var commands = app.Services.GetRequiredService<ICommandPipeline>();
            var queries = app.Services.GetRequiredService<IQueryPipeline>();
            request.User = Guest();
            _filteredCommand = await commands.Execute(new FilteredCommand(), scope.ServiceProvider);
            request.User = Guest();
            _providedCommand = await commands.Execute(new ProvidedCommand(), scope.ServiceProvider);
            request.User = Guest();
            _validatedCommand = await commands.Execute(new ValidatedCommand(), scope.ServiceProvider);
            request.User = Guest();
            _filteredQuery = await queries.Perform(
                new FullyQualifiedQueryName($"{typeof(GuestReadModel).FullName}.All"), QueryArguments.Empty, Paging.NotPaged, Sorting.None, scope.ServiceProvider);
            request.User = Member("evaluated");
            _memberQuery = await queries.Perform(
                new FullyQualifiedQueryName($"{typeof(MemberReadModel).FullName}.All"), QueryArguments.Empty, Paging.NotPaged, Sorting.None, scope.ServiceProvider);
            _provided = ProvidedCommand.Provided - _providedBefore;
            _policiesEvaluated = scope.ServiceProvider.GetRequiredService<CallerPolicy>().Evaluations;
        }
        finally
        {
            ProvidedCommand.Request = null;
            requests.Current = null;
        }
    }

    [Fact] void should_deny_the_command_changed_by_an_ordinary_filter() => _filteredCommand.IsAuthorized.ShouldBeFalse();
    [Fact] void should_deny_the_command_changed_by_provide() => _providedCommand.IsAuthorized.ShouldBeFalse();
    [Fact] void should_deny_the_command_changed_by_a_validator() => _validatedCommand.IsAuthorized.ShouldBeFalse();
    [Fact] void should_deny_the_query_changed_by_an_ordinary_filter() => _filteredQuery.IsAuthorized.ShouldBeFalse();
    [Fact] void should_deny_the_non_opted_query_after_a_principal_swap() => _memberQuery.IsAuthorized.ShouldBeFalse();
    [Fact] void should_invoke_provide_after_the_verdict() => _provided.ShouldEqual(1);
    [Fact] void should_evaluate_all_policies_before_the_identity_changes() => _policiesEvaluated.ShouldEqual(5);
    [Fact] void should_not_invoke_the_filtered_command_handler() => FilteredCommand.Handled.ShouldEqual(_filteredCommandsBefore);
    [Fact] void should_not_invoke_the_provided_command_handler() => ProvidedCommand.Handled.ShouldEqual(_providedCommandsBefore);
    [Fact] void should_not_invoke_the_validated_command_handler() => ValidatedCommand.Handled.ShouldEqual(_validatedCommandsBefore);
    [Fact] void should_not_invoke_the_guest_query_performer() => GuestReadModel.Performed.ShouldEqual(_guestQueriesBefore);
    [Fact] void should_not_invoke_the_member_query_performer() => MemberReadModel.Performed.ShouldEqual(_memberQueriesBefore);

    static ClaimsPrincipal Guest() => new(new ClaimsIdentity());
    static ClaimsPrincipal Member(string name) => new(new ClaimsIdentity([new Claim(ClaimTypes.Name, name)], "test"));

    public class CallerPolicy : IAuthorizationPolicy
    {
        public int Evaluations { get; private set; }

        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            Evaluations++;
            return ValueTask.FromResult(context.Principal.Identity?.IsAuthenticated != true || context.Principal.Identity.Name == "evaluated");
        }
    }

    class MutatingCommandFilter : ICommandFilter
    {
        public Task<CommandResult> OnExecution(CommandContext context)
        {
            if (context.Command is FilteredCommand)
            {
                context.ServiceProvider.GetRequiredService<IHttpRequestContextAccessor>().Current.User.AddIdentity(
                    new ClaimsIdentity([new Claim(ClaimTypes.Name, "unevaluated")], "test"));
            }

            return Task.FromResult(CommandResult.Success(context.CorrelationId));
        }
    }

    class MutatingQueryFilter : IQueryFilter
    {
        public Task<QueryResult> OnPerform(QueryContext context)
        {
            var request = context.ServiceProvider.GetRequiredService<IHttpRequestContextAccessor>().Current;
            if (context.Name.Value == $"{typeof(GuestReadModel).FullName}.All")
            {
                request.User.AddIdentity(new ClaimsIdentity([new Claim(ClaimTypes.Name, "unevaluated")], "test"));
            }
            else
            {
                request.User = Member("unevaluated");
            }

            return Task.FromResult(QueryResult.Success(context.CorrelationId));
        }
    }

    [Command]
    [Authorize(Policy = "Guest")]
    public record FilteredCommand
    {
        static int _handled;
        public static int Handled => Volatile.Read(ref _handled);
        public void Handle() => Interlocked.Increment(ref _handled);
    }

    [Command]
    [Authorize(Policy = "Guest")]
    public record ProvidedCommand
    {
        static int _handled;
        static int _provided;
        public static int Handled => Volatile.Read(ref _handled);
        public static int Provided => Volatile.Read(ref _provided);
        public static IHttpRequestContext? Request { get; set; }
        public void Provide()
        {
            Interlocked.Increment(ref _provided);
            Request.User.AddIdentity(new ClaimsIdentity([new Claim(ClaimTypes.Name, "unevaluated")], "test"));
        }
        public void Handle() => Interlocked.Increment(ref _handled);
    }

    [Command]
    [Authorize(Policy = "Guest")]
    public record ValidatedCommand
    {
        static int _handled;
        public static int Handled => Volatile.Read(ref _handled);
        public void Handle() => Interlocked.Increment(ref _handled);
    }

    [ReadModel]
    [Authorize(Policy = "Guest")]
    public record GuestReadModel(string Value)
    {
        static int _performed;
        public static int Performed => Volatile.Read(ref _performed);
        public static GuestReadModel All()
        {
            Interlocked.Increment(ref _performed);
            return new("unexpected");
        }
    }

    [ReadModel]
    [Authorize(Policy = "Member")]
    public record MemberReadModel(string Value)
    {
        static int _performed;
        public static int Performed => Volatile.Read(ref _performed);
        public static MemberReadModel All()
        {
            Interlocked.Increment(ref _performed);
            return new("unexpected");
        }
    }
}
