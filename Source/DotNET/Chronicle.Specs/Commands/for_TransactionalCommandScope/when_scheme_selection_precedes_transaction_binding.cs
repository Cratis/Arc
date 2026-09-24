// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Http;
using Cratis.Arc.Tenancy;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope;

[Collection("UsesCurrentDirectory")]
public class when_scheme_selection_precedes_transaction_binding : Specification
{
    string? _preboundTenant;
    string? _begunTenant;
    string? _restoredTenant;
    bool _sameManager;
    bool _requestServicesRestored;
    bool _authorized;
    bool _succeeded;
    int _unitOfWorkBegins;
    IUnitOfWorkManager? _selectedManager;
    IUnitOfWork? _selectedUnit;
    int _enrolledEvents;
    string? _enrolledEventTenant;

    async Task Because()
    {
        var availableTypes = Substitute.For<ITypes>();
        availableTypes.All.Returns([typeof(TenantCommand), typeof(NestedCommand)]);
        var selected = Principal("tenant-B", "Special");
        var nestedSelected = Principal("tenant-C", "Other");
        var authentication = Substitute.For<IAuthenticationService>();
        authentication.AuthenticateAsync(Arg.Any<HttpContext>(), "Special")
            .Returns(Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(selected, "Special"))));
        authentication.AuthenticateAsync(Arg.Any<HttpContext>(), "Other")
            .Returns(Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(nestedSelected, "Other"))));
        var schemes = Substitute.For<IAuthenticationSchemeProvider>();
        schemes.GetSchemeAsync("Special")
            .Returns(Task.FromResult<AuthenticationScheme?>(new AuthenticationScheme("Special", "Special", typeof(IAuthenticationHandler))));
        schemes.GetSchemeAsync("Other")
            .Returns(Task.FromResult<AuthenticationScheme?>(new AuthenticationScheme("Other", "Other", typeof(IAuthenticationHandler))));

        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc(options => options.Tenancy.ResolverType = TenantResolverType.Claim);
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("TenantPolicy", policy => policy.AddAuthenticationSchemes("Special").RequireClaim("tenant_id", "tenant-B"))
            .AddPolicy("NestedTenantPolicy", policy => policy.AddAuthenticationSchemes("Other").RequireClaim("tenant_id", "tenant-C"));
        builder.Services.AddSingleton(authentication);
        builder.Services.AddSingleton(schemes);
        builder.Services.AddSingleton<ICommandKeys>(Substitute.For<ICommandKeys>());
        builder.Services.AddSingleton<IInstancesOf<ICommandContextValuesProvider>>(new KnownInstancesOf<ICommandContextValuesProvider>([]));
        builder.Services.AddSingleton<IInstancesOf<ICommandExecutionScope>>(
            new KnownInstancesOf<ICommandExecutionScope>([new TransactionalCommandScope()]));
        builder.Services.AddSingleton<ICommandHandlerProviders>(_ => new CommandHandlerProviders(
            new KnownInstancesOf<ICommandHandlerProvider>([new CommandHandlerProvider(availableTypes)])));
        var eventTypes = Substitute.For<IEventTypes>();
        eventTypes.HasFor(typeof(TenantBoundWorkRecorded)).Returns(true);
        builder.Services.AddSingleton(eventTypes);
        var concurrency = Substitute.For<IConcurrencyScopeStrategies>();
        builder.Services.AddSingleton(concurrency);
        builder.Services.AddScoped<IUnitOfWorkManager>(services =>
        {
            var tenant = services.GetRequiredService<ITenantIdAccessor>().Current.Value;
            var manager = Substitute.For<IUnitOfWorkManager>();
            var unit = Substitute.For<IUnitOfWork>();
            manager.HasCurrent.Returns(false);
            manager.Begin(Arg.Any<CorrelationId>()).Returns(_ =>
            {
                Interlocked.Increment(ref _unitOfWorkBegins);
                _begunTenant = tenant;
                _selectedManager = manager;
                _selectedUnit = unit;
                return unit;
            });
            return manager;
        });
        builder.Services.AddScoped<IEventLog>(_ =>
        {
            var eventLog = Substitute.For<IEventLog>();
            eventLog.Id.Returns(EventSequenceId.Log);
            return eventLog;
        });

        await using var app = builder.Build();
        await using var requestScope = app.Services.CreateAsyncScope();
        var native = new DefaultHttpContext { RequestServices = requestScope.ServiceProvider, User = Principal("tenant-A", "Default") };
        var aspContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();
        aspContextAccessor.HttpContext = native;
        var arcContextAccessor = app.Services.GetRequiredService<IHttpRequestContextAccessor>();
        arcContextAccessor.Current = new Cratis.Arc.AspNetCore.Http.AspNetCoreHttpRequestContext(native);
        var tenantIds = app.Services.GetRequiredService<ITenantIdAccessor>();
        _preboundTenant = tenantIds.Current.Value;
        var oldManager = requestScope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
        try
        {
            var result = await app.Services.GetRequiredService<ICommandPipeline>().Execute(new TenantCommand());
            _authorized = result.IsAuthorized;
            _succeeded = result.IsSuccess;
            _restoredTenant = tenantIds.Current.Value;
            _requestServicesRestored = ReferenceEquals(native.RequestServices, requestScope.ServiceProvider);
            _sameManager = ReferenceEquals(oldManager, _selectedManager);
            var events = _selectedUnit?.ReceivedCalls().Where(call => call.GetMethodInfo().Name == "AddEvent").ToArray() ?? [];
            _enrolledEvents = events.Length;
            _enrolledEventTenant = events.SingleOrDefault()?.GetArguments().OfType<TenantBoundWorkRecorded>().SingleOrDefault()?.Tenant;
        }
        finally
        {
            arcContextAccessor.Current = null;
            aspContextAccessor.HttpContext = null;
        }
    }

    [Fact] void should_have_prebound_the_default_request_manager_to_a() => _preboundTenant.ShouldEqual("tenant-A");
    [Fact] void should_admit_the_scheme_selected_command() => _authorized.ShouldBeTrue();
    [Fact] void should_complete_the_command_transaction() => _succeeded.ShouldBeTrue();
    [Fact] void should_begin_the_real_transactional_scope_with_b() => _begunTenant.ShouldEqual("tenant-B");
    [Fact] void should_handle_under_the_selected_tenant() => TenantCommand.HandledTenant.ShouldEqual("tenant-B");
    [Fact] void should_not_reuse_the_default_requests_unit_of_work_manager() => _sameManager.ShouldBeFalse();
    [Fact] void should_restore_the_original_tenant_cache() => _restoredTenant.ShouldEqual("tenant-A");
    [Fact] void should_restore_http_request_services() => _requestServicesRestored.ShouldBeTrue();
    [Fact] void should_enroll_only_the_outer_returned_event() => _enrolledEvents.ShouldEqual(1);
    [Fact] void should_enroll_the_event_with_the_selected_tenant() => _enrolledEventTenant.ShouldEqual("tenant-B");
    [Fact] void should_reject_the_nested_identity_change_before_it_begins_a_transaction() => _unitOfWorkBegins.ShouldEqual(1);
    [Fact] void should_not_authorize_the_different_nested_actor() => TenantCommand.NestedAuthorized.ShouldBeFalse();
    [Fact] void should_not_expose_the_nested_transaction_identity_failure() => TenantCommand.NestedFailure.ShouldBeEmpty();
    [Fact] void should_not_invoke_the_nested_handler() => NestedCommand.Handled.ShouldEqual(0);

    static ClaimsPrincipal Principal(string tenant, string scheme) => new(new ClaimsIdentity([new Claim("tenant_id", tenant)], scheme));

    [Command]
    [Cratis.Arc.Authorization.Authorize(Policy = "TenantPolicy")]
    public record TenantCommand
    {
        public static string? HandledTenant { get; private set; }

        public static bool NestedAuthorized { get; private set; }

        public static string? NestedFailure { get; private set; }

        public async Task<EventForEventSourceId> Handle(ITenantIdAccessor tenants, ICommandPipeline pipeline)
        {
            HandledTenant = tenants.Current.Value;
            var nested = await pipeline.Execute(new NestedCommand());
            NestedAuthorized = nested.IsAuthorized;
            NestedFailure = nested.AuthorizationFailureReason;
            return new EventForEventSourceId(EventSourceId.New(), new TenantBoundWorkRecorded(tenants.Current.Value));
        }
    }

    [EventType("e3c8f0d1-7a59-44af-8e92-b41d49aee601")]
    public record TenantBoundWorkRecorded(string Tenant);

    [Command]
    [Cratis.Arc.Authorization.Authorize(Policy = "NestedTenantPolicy")]
    public record NestedCommand
    {
        static int _handled;

        public static int Handled => Volatile.Read(ref _handled);

        public void Handle() => Interlocked.Increment(ref _handled);
    }
}
