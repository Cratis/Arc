// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

[Collection("UsesCurrentDirectory")]
public class when_a_native_policy_evaluates_guests : Specification
{
    CommandResult _allowed;
    CommandResult _default;
    CommandResult _withRole;
    CommandResult _withBareAuthorization;
    CommandResult _withDefaultPolicy;
    CommandResult _denied;
    CommandResult _thrown;
    CommandResult _authenticated;
    QueryResult _guestQuery;
    QueryResult _guestObservable;
    QueryResult _authenticatedQuery;
    QueryResult _guestCatalog;
    QueryResult _deniedCatalog;
    QueryResult _guestPrivateCatalog;
    QueryResult _adminPrivateCatalog;
    CommandResult _guestValidation;
    CommandResult _roleValidation;
    int _allowedCalls;
    int _defaultCalls;
    int _throwCalls;
    int _executed;
    readonly CapturingLogger _policyLogger = new();

    async Task Because()
    {
        PermittingGuests.Calls = 0;
        DefaultPolicy.Calls = 0;
        ThrowingPolicy.Calls = 0;
        GuestCommand.Handled = 0;
        ThrowingCommand.Handled = 0;
        var builder = ArcApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.Services.AddArcAuthorizationPolicy<PermittingGuests>("Guest", evaluatesAnonymous: true);
        builder.Services.AddArcAuthorizationPolicy<DefaultPolicy>("Default");
        builder.Services.AddArcAuthorizationPolicy<DenyingGuests>("Deny", evaluatesAnonymous: true);
        builder.Services.AddArcAuthorizationPolicy<ThrowingPolicy>("Throw", evaluatesAnonymous: true);
        builder.Services.AddSingleton<ILogger<AuthorizationEvaluation>>(_policyLogger);
        builder.Services.AddSingleton(Substitute.For<ICommandKeys>());
        var available = Substitute.For<ITypes>();
        available.All.Returns([typeof(GuestCommand), typeof(DefaultCommand), typeof(RoleCommand), typeof(BareCommand),
            typeof(DeniedCommand), typeof(ThrowingCommand), typeof(GuestAndDefaultCommand), typeof(GuestReadModel), typeof(RoleReadModel)]);
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
                [$"{typeof(GuestReadModel).FullName}.Stream"] = typeof(GuestReadModel),
                [$"{typeof(RoleReadModel).FullName}.Public"] = typeof(RoleReadModel),
                [$"{typeof(RoleReadModel).FullName}.Denied"] = typeof(RoleReadModel),
                [$"{typeof(RoleReadModel).FullName}.Private"] = typeof(RoleReadModel)
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
        var execution = app.Services.GetRequiredService<ISystemExecution>();
        var commands = app.Services.GetRequiredService<ICommandPipeline>();
        var queries = app.Services.GetRequiredService<QueryPipeline>();
        using (execution.As(new ClaimsPrincipal(new ClaimsIdentity([new Claim("untrusted", "ignored")]))))
        {
            _allowed = await commands.Execute(new GuestCommand());
            _guestValidation = await commands.Validate(new GuestCommand());
            _roleValidation = await commands.Validate(new RoleCommand());
            _default = await commands.Execute(new DefaultCommand());
            _withRole = await commands.Execute(new RoleCommand());
            _withBareAuthorization = await commands.Execute(new BareCommand());
            _withDefaultPolicy = await commands.Execute(new GuestAndDefaultCommand());
            _denied = await commands.Execute(new DeniedCommand());
            _thrown = await commands.Execute(new ThrowingCommand());
            _guestQuery = await queries.PerformHosted(
                new FullyQualifiedQueryName($"{typeof(GuestReadModel).FullName}.All"),
                QueryArguments.Empty,
                Paging.NotPaged,
                Sorting.None,
                app.Services,
                CancellationToken.None);
            _guestObservable = await queries.PerformHosted(
                new FullyQualifiedQueryName($"{typeof(GuestReadModel).FullName}.Stream"),
                QueryArguments.Empty,
                Paging.NotPaged,
                Sorting.None,
                app.Services,
                CancellationToken.None);
            _guestCatalog = await queries.PerformHosted(
                new FullyQualifiedQueryName($"{typeof(RoleReadModel).FullName}.Public"),
                QueryArguments.Empty,
                Paging.NotPaged,
                Sorting.None,
                app.Services,
                CancellationToken.None);
            _deniedCatalog = await queries.PerformHosted(
                new FullyQualifiedQueryName($"{typeof(RoleReadModel).FullName}.Denied"),
                QueryArguments.Empty,
                Paging.NotPaged,
                Sorting.None,
                app.Services,
                CancellationToken.None);
            _guestPrivateCatalog = await queries.PerformHosted(
                new FullyQualifiedQueryName($"{typeof(RoleReadModel).FullName}.Private"),
                QueryArguments.Empty,
                Paging.NotPaged,
                Sorting.None,
                app.Services,
                CancellationToken.None);
        }

        _allowedCalls = PermittingGuests.Calls;
        _defaultCalls = DefaultPolicy.Calls;
        _throwCalls = ThrowingPolicy.Calls;
        _executed = GuestCommand.Handled;
        using (execution.As(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "member")], "test"))))
        {
            _authenticated = await commands.Execute(new GuestCommand());
            _authenticatedQuery = await queries.PerformHosted(
                new FullyQualifiedQueryName($"{typeof(GuestReadModel).FullName}.All"),
                QueryArguments.Empty,
                Paging.NotPaged,
                Sorting.None,
                app.Services,
                CancellationToken.None);
        }

        using (execution.As(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Admin")], "test"))))
        {
            _adminPrivateCatalog = await queries.PerformHosted(
                new FullyQualifiedQueryName($"{typeof(RoleReadModel).FullName}.Private"),
                QueryArguments.Empty,
                Paging.NotPaged,
                Sorting.None,
                app.Services,
                CancellationToken.None);
        }
    }

    [Fact] void should_allow_the_guest_command() => _allowed.IsAuthorized.ShouldBeTrue();
    [Fact] void should_run_the_guest_command_once() => _executed.ShouldEqual(1);
    [Fact] void should_evaluate_the_guest_policy_for_a_command_and_a_query() => (_allowedCalls >= 2).ShouldBeTrue();
    [Fact] void should_not_evaluate_the_default_policy_for_a_guest() => _defaultCalls.ShouldEqual(0);
    [Fact] void should_deny_a_default_policy_for_a_guest() => _default.IsAuthorized.ShouldBeFalse();
    [Fact] void should_deny_a_guest_with_a_role() => _withRole.IsAuthorized.ShouldBeFalse();
    [Fact] void should_allow_guest_validation() => _guestValidation.IsAuthorized.ShouldBeTrue();
    [Fact] void should_deny_role_protected_validation() => _roleValidation.IsAuthorized.ShouldBeFalse();
    [Fact] void should_deny_a_guest_with_a_bare_authorize() => _withBareAuthorization.IsAuthorized.ShouldBeFalse();
    [Fact] void should_deny_a_guest_with_a_non_opted_in_policy() => _withDefaultPolicy.IsAuthorized.ShouldBeFalse();
    [Fact] void should_deny_when_the_guest_policy_returns_false() => _denied.IsAuthorized.ShouldBeFalse();
    [Fact] void should_deny_when_the_policy_throws() => _thrown.IsAuthorized.ShouldBeFalse();
    [Fact] void should_reach_the_throwing_policy() => _throwCalls.ShouldEqual(1);
    [Fact] void should_log_the_guest_policy_exception() => _policyLogger.Error.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();
    [Fact] void should_log_the_failed_target() => _policyLogger.Target.ShouldEqual(nameof(ThrowingCommand));
    [Fact] void should_not_run_the_throwing_command() => ThrowingCommand.Handled.ShouldEqual(0);
    [Fact] void should_allow_the_guest_query() => _guestQuery.IsAuthorized.ShouldBeTrue();
    [Fact] void should_admit_the_guest_observable() => _guestObservable.IsAuthorized.ShouldBeTrue();
    [Fact] void should_have_no_query_error() => _guestQuery.ExceptionMessages.ShouldBeEmpty();
    [Fact] void should_preserve_authenticated_commands() => _authenticated.IsAuthorized.ShouldBeTrue();
    [Fact] void should_preserve_authenticated_queries() => _authenticatedQuery.IsAuthorized.ShouldBeTrue();
    [Fact] void should_let_the_public_method_replace_the_type_role_for_a_guest() => _guestCatalog.IsAuthorized.ShouldBeTrue();
    [Fact] void should_still_obey_the_public_methods_policy_verdict() => _deniedCatalog.IsAuthorized.ShouldBeFalse();
    [Fact] void should_require_the_type_role_on_other_methods_for_a_guest() => _guestPrivateCatalog.IsAuthorized.ShouldBeFalse();
    [Fact] void should_allow_the_type_role_on_other_methods_for_an_admin() => _adminPrivateCatalog.IsAuthorized.ShouldBeTrue();

    [Command]
    [Authorize(Policy = "Guest")]
    public record GuestCommand
    {
        public static int Handled;
        public void Handle() => Interlocked.Increment(ref Handled);
    }

    [Command]
    [Authorize(Policy = "Default")]
    public record DefaultCommand { public void Handle() { } }

    [Command]
    [Authorize(Policy = "Guest", Roles = "Admin")]
    public record RoleCommand { public void Handle() { } }

    [Command]
    [Authorize(Policy = "Guest")]
    [Authorize]
    public record BareCommand { public void Handle() { } }

    [Command]
    [Authorize(Policy = "Guest")]
    [Authorize(Policy = "Default")]
    public record GuestAndDefaultCommand { public void Handle() { } }

    [Command]
    [Authorize(Policy = "Deny")]
    public record DeniedCommand { public void Handle() { } }

    [Command]
    [Authorize(Policy = "Throw")]
    public record ThrowingCommand
    {
        public static int Handled;
        public void Handle() => Interlocked.Increment(ref Handled);
    }

    [ReadModel]
    [Authorize(Policy = "Guest")]
    public record GuestReadModel(string Value)
    {
        public static GuestReadModel All() => new("allowed");
        public static ISubject<IEnumerable<GuestReadModel>> Stream() => new ReplaySubject<IEnumerable<GuestReadModel>>();
    }

    [ReadModel]
    [Authorize(Roles = "Admin")]
    public record RoleReadModel(string Value)
    {
        [Authorize(Policy = "Guest")]
        public static RoleReadModel Public() => new("public");

        [Authorize(Policy = "Deny")]
        public static RoleReadModel Denied() => new("denied");

        public static RoleReadModel Private() => new("private");
    }

    public class PermittingGuests : IAuthorizationPolicy
    {
        public static int Calls;
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Calls);
            return ValueTask.FromResult(!context.Principal.HasClaim("untrusted", "ignored") &&
                (context.Principal.Identity?.IsAuthenticated == false || context.Principal.Identity?.IsAuthenticated == true));
        }
    }

    public class DefaultPolicy : IAuthorizationPolicy
    {
        public static int Calls;
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Calls);
            return ValueTask.FromResult(true);
        }
    }

    public class DenyingGuests : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken) => ValueTask.FromResult(false);
    }

    class CapturingLogger : ILogger<AuthorizationEvaluation>
    {
        public Exception? Error { get; private set; }
        public string? Target { get; private set; }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                Error = exception;
                Target = (state as IEnumerable<KeyValuePair<string, object?>>)?.FirstOrDefault(pair => pair.Key == "Target").Value?.ToString();
            }
        }
    }

    public class ThrowingPolicy : IAuthorizationPolicy
    {
        public static int Calls;
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Calls);
            throw new InvalidAuthorizationConfiguration("Policy failed.");
        }
    }
}
