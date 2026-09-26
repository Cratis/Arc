// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator;

[Collection("UsesCurrentDirectory")]
public class when_hosting_commands_with_fallback_policies_and_schemes : Specification
{
    CommandResult _allowedPolicy;
    CommandResult _deniedPolicy;
    CommandResult _allowedScheme;
    CommandResult _deniedScheme;
    CommandResult _explicitPolicy;
    CommandResult _validatedScheme;
    int _buildsBeforeDeniedPolicy;
    int _buildsAfterDeniedPolicy;
    int _buildsBeforeDeniedScheme;
    int _buildsAfterDeniedScheme;
    int _policyHandles;
    int _schemeHandles;
    int _explicitHandles;
    string? _selectedNameInValues;

    async Task Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.Services.AddArcAuthorizationPolicy<AllowedPolicy>("Allowed");
        var probe = new ObservingValuesBuilder();
        builder.Services.AddSingleton<ICommandContextValuesBuilder>(probe);
        builder.Services.AddSingleton<IScopedCommandContextValuesBuilder>(probe);
        var handlers = Substitute.For<ICommandHandlerProviders>();
        var available = new[] { typeof(PolicyCommand), typeof(SchemeCommand), typeof(ExplicitCommand) }
            .Select(type => (ICommandHandler)new ModelBoundCommandHandler(type, type.GetMethod("Handle")!))
            .ToArray();
        handlers.Handlers.Returns(available);
        handlers.TryGetHandlerFor(Arg.Any<object>(), out var _).Returns(call =>
        {
            var found = available.FirstOrDefault(handler => handler.CommandType == call[0]!.GetType());
            call[1] = found;
            return found is not null;
        });
        builder.Services.AddSingleton(handlers);
        var schemes = Substitute.For<IAuthenticationSchemeProvider>();
        schemes.GetSchemeAsync("Selected").Returns(Task.FromResult<AuthenticationScheme?>(
            new AuthenticationScheme("Selected", "Selected", typeof(IAuthenticationHandler))));
        builder.Services.AddSingleton(schemes);
        var authentication = Substitute.For<IAuthenticationService>();
        var selectedIsAllowed = true;
        authentication.AuthenticateAsync(Arg.Any<HttpContext>(), "Selected").Returns(_ => Task.FromResult(
            selectedIsAllowed
                ? AuthenticateResult.Success(new AuthenticationTicket(
                    new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "selected")], "Selected")), "Selected"))
                : AuthenticateResult.Fail("Selected scheme rejected caller")));
        builder.Services.AddSingleton(authentication);

        await using var app = builder.Build();
        await using var scope = app.Services.CreateAsyncScope();
        var request = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        var http = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        http.HttpContext = request;
        var pipeline = (CommandPipeline)scope.ServiceProvider.GetRequiredService<ICommandPipeline>();
        var accessor = scope.ServiceProvider.GetRequiredService<ICurrentPrincipalOverride>();
        var authorized = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "caller")], "Default"));
        PolicyCommand.Handles = 0;
        SchemeCommand.Handles = 0;
        ExplicitCommand.Handles = 0;

        request.User = authorized;
        using (accessor.BeginScope(authorized))
        {
            _allowedPolicy = await pipeline.ExecuteHosted(new PolicyCommand(), scope.ServiceProvider, null, CancellationToken.None);
            _explicitPolicy = await pipeline.ExecuteHosted(new ExplicitCommand(), scope.ServiceProvider, null, CancellationToken.None);
        }

        _buildsBeforeDeniedPolicy = probe.Builds;
        request.User = new ClaimsPrincipal(new ClaimsIdentity());
        using (accessor.BeginScope(request.User))
        {
            _deniedPolicy = await pipeline.ExecuteHosted(new PolicyCommand(), scope.ServiceProvider, null, CancellationToken.None);
            _buildsAfterDeniedPolicy = probe.Builds;
            _allowedScheme = await pipeline.ExecuteHosted(new SchemeCommand(), scope.ServiceProvider, null, CancellationToken.None);
            _validatedScheme = await pipeline.ValidateHosted(new SchemeCommand(), scope.ServiceProvider, null, CancellationToken.None);
        }

        _selectedNameInValues = probe.SelectedName;
        _buildsBeforeDeniedScheme = probe.Builds;
        selectedIsAllowed = false;
        request.User = authorized;
        using (accessor.BeginScope(authorized))
        {
            _deniedScheme = await pipeline.ExecuteHosted(new SchemeCommand(), scope.ServiceProvider, null, CancellationToken.None);
        }

        _buildsAfterDeniedScheme = probe.Builds;
        _policyHandles = PolicyCommand.Handles;
        _schemeHandles = SchemeCommand.Handles;
        _explicitHandles = ExplicitCommand.Handles;
    }

    [Fact] void should_allow_the_policy_fallback() => _allowedPolicy.IsSuccess.ShouldBeTrue();
    [Fact] void should_reject_an_anonymous_policy_caller_before_building_context_values() => _deniedPolicy.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_build_context_values_for_the_denied_policy_caller() => _buildsAfterDeniedPolicy.ShouldEqual(_buildsBeforeDeniedPolicy);
    [Fact] void should_allow_the_scheme_fallback_without_late_scheme_selection() => _allowedScheme.IsSuccess.ShouldBeTrue();
    [Fact] void should_use_the_selected_scheme_before_building_context_values() => _selectedNameInValues.ShouldEqual("selected");
    [Fact] void should_prepare_a_scheme_fallback_before_validation() => _validatedScheme.IsSuccess.ShouldBeTrue();
    [Fact] void should_reject_a_failed_scheme_even_if_the_default_caller_is_authenticated() => _deniedScheme.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_build_context_values_for_a_failed_scheme() => _buildsAfterDeniedScheme.ShouldEqual(_buildsBeforeDeniedScheme);
    [Fact] void should_not_execute_denied_commands() => (_policyHandles, _schemeHandles, _explicitHandles).ShouldEqual((1, 1, 1));
    [Fact] void should_keep_explicit_policy_authorization() => _explicitPolicy.IsSuccess.ShouldBeTrue();

    public record PolicyCommand
    {
        public static int Handles { get; set; }
        public void Handle() => Handles++;
    }

    public record SchemeCommand
    {
        public static int Handles { get; set; }
        public void Handle() => Handles++;
    }

    [Authorize(Policy = "Allowed")]
    public record ExplicitCommand
    {
        public static int Handles { get; set; }
        public void Handle() => Handles++;
    }

    public class CommandBaseline : IFallbackAuthorizationEvaluator
    {
        public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(Type type) => type == typeof(PolicyCommand)
            ? [AuthorizationRequirement.FromAttribute(null, "Allowed", null)]
            : type == typeof(SchemeCommand)
                ? [AuthorizationRequirement.FromAttribute(null, null, "Selected")]
                : [];

        public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(MethodInfo method) => [];
    }

    public class AllowedPolicy : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken) =>
            ValueTask.FromResult(context.Principal.Identity?.IsAuthenticated == true);
    }

    class ObservingValuesBuilder : IScopedCommandContextValuesBuilder
    {
        public int Builds { get; private set; }
        public string? SelectedName { get; private set; }

        public CommandContextValues Build(object command)
        {
            Builds++;
            return new CommandContextValues();
        }

        public CommandContextValues Build(object command, IServiceProvider services)
        {
            Builds++;
            SelectedName = services.GetRequiredService<ICurrentPrincipalAccessor>().Current?.Identity?.Name;
            return new CommandContextValues();
        }
    }
}
