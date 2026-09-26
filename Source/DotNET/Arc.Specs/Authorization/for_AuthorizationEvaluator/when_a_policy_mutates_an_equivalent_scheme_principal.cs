// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator;

public class when_a_policy_mutates_an_equivalent_scheme_principal : Specification
{
    bool _commandAllowed;
    bool _queryAllowed;
    bool _selectedWasDistinct;
    bool _principalChanged;
    bool _laterPolicyPassed;
    bool _ambientUnchanged;

    async Task Because()
    {
        var anonymous = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymous.GetEnumerator().Returns(_ => new IAnonymousEvaluator[] { new AnonymousEvaluator() }.AsEnumerable().GetEnumerator());
        var attributes = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributes.GetEnumerator().Returns(_ => new IAuthorizationAttributeEvaluator[] { new AuthorizationAttributeEvaluator() }.AsEnumerable().GetEnumerator());
        var original = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "caller")], "Cookies"));
        var httpContext = new DefaultHttpContext { User = original };
        var http = Substitute.For<IHttpContextAccessor>();
        http.HttpContext.Returns(httpContext);
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(_ => httpContext.User);
        var evaluator = new AuthorizationEvaluator(accessor, anonymous, attributes);
        var schemes = Substitute.For<IAuthenticationSchemeProvider>();
        schemes.GetSchemeAsync("Cookies").Returns(Task.FromResult<AuthenticationScheme?>(new AuthenticationScheme("Cookies", "Cookies", typeof(IAuthenticationHandler))));
        var authentication = Substitute.For<IAuthenticationService>();
        authentication.AuthenticateAsync(httpContext, "Cookies").Returns(_ => Task.FromResult(
            AuthenticateResult.Success(new AuthenticationTicket(original, "Cookies"))));
        await using var services = new ServiceCollection()
            .AddSingleton(schemes)
            .AddSingleton(authentication)
            .AddSingleton(http)
            .AddSingleton<ElevatePolicy>()
            .AddSingleton<AdminPolicy>()
            .BuildServiceProvider();
        var runtime = new AspNetAuthorizationPolicyRuntime(new ArcAuthorizationPolicyRuntime(
        [
            new AuthorizationPolicyRegistration("Elevate", typeof(ElevatePolicy)),
            new AuthorizationPolicyRegistration("Admin", typeof(AdminPolicy))
        ]));
        var evaluation = new AuthorizationEvaluation(new AuthorizationDeclarations(anonymous, attributes), evaluator, accessor, runtime);
        var commandPlan = await evaluation.Prepare(typeof(ProtectedCommand), services, CancellationToken.None);
        _selectedWasDistinct = !ReferenceEquals(original, commandPlan.SelectedPrincipal);
        _principalChanged = commandPlan.PrincipalChanged;
        _commandAllowed = await evaluation.IsAuthorized(typeof(ProtectedCommand), new ProtectedCommand(), services, CancellationToken.None);
        var method = typeof(ProtectedQuery).GetMethod(nameof(ProtectedQuery.All))!;
        _queryAllowed = await evaluation.IsAuthorized(method, new object(), services, CancellationToken.None);
        _laterPolicyPassed = services.GetRequiredService<AdminPolicy>().WasAllowed;
        _ambientUnchanged = !original.IsInRole("Admin");
    }

    [Fact] void should_select_a_distinct_but_equivalent_principal() => _selectedWasDistinct.ShouldBeTrue();
    [Fact] void should_not_create_a_selected_scope() => _principalChanged.ShouldBeFalse();
    [Fact] void should_run_the_later_policy_against_the_mutated_selection() => _laterPolicyPassed.ShouldBeTrue();
    [Fact] void should_not_mutate_the_ambient_caller() => _ambientUnchanged.ShouldBeTrue();
    [Fact] void should_deny_the_command_verdict() => _commandAllowed.ShouldBeFalse();
    [Fact] void should_deny_the_query_verdict() => _queryAllowed.ShouldBeFalse();

    [Authorize(Policy = "Elevate", AuthenticationSchemes = "Cookies")]
    [Authorize(Policy = "Admin")]
    public record ProtectedCommand;

    public static class ProtectedQuery
    {
        [Authorize(Policy = "Elevate", AuthenticationSchemes = "Cookies")]
        [Authorize(Policy = "Admin")]
        public static void All() { }
    }

    public class ElevatePolicy : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            context.Principal.AddIdentity(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Admin")], "Cookies"));
            return ValueTask.FromResult(true);
        }
    }

    public class AdminPolicy : IAuthorizationPolicy
    {
        public bool WasAllowed { get; private set; }

        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            WasAllowed = context.Principal.IsInRole("Admin");
            return ValueTask.FromResult(WasAllowed);
        }
    }
}
