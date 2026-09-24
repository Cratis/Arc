// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator;

public class when_a_scheme_selects_an_equivalent_default_principal : Specification
{
    bool _allowed;
    bool _policyEvaluated;
    bool _selectedPrincipalWasDistinct;
    bool _principalChanged;
    Exception? _differentPrincipalFailure;

    async Task Because()
    {
        var anonymous = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymous.GetEnumerator().Returns(_ => new IAnonymousEvaluator[] { new AnonymousEvaluator() }.AsEnumerable().GetEnumerator());
        var attributes = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributes.GetEnumerator().Returns(_ => new IAuthorizationAttributeEvaluator[] { new AuthorizationAttributeEvaluator() }.AsEnumerable().GetEnumerator());
        var original = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "caller"), new Claim(ClaimTypes.Role, "Admin")], "Cookies"));
        var httpContext = new DefaultHttpContext { User = original };
        var http = Substitute.For<IHttpContextAccessor>();
        http.HttpContext.Returns(httpContext);
        var current = original;
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(_ => current);
        var builtIn = new AuthorizationEvaluator(accessor, anonymous, attributes);
        var legacy = new DelegatingEvaluator(builtIn, () =>
        {
            current = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "other"), new Claim(ClaimTypes.Role, "Admin")], "Cookies"));
            _differentPrincipalFailure = Catch.Exception(() => builtIn.IsAuthorized(typeof(ProtectedCommand)));
            current = original;
        });
        var schemes = Substitute.For<IAuthenticationSchemeProvider>();
        schemes.GetSchemeAsync("Cookies").Returns(Task.FromResult<AuthenticationScheme?>(new AuthenticationScheme("Cookies", "Cookies", typeof(IAuthenticationHandler))));
        var authentication = Substitute.For<IAuthenticationService>();
        authentication.AuthenticateAsync(httpContext, "Cookies").Returns(_ => Task.FromResult(
            AuthenticateResult.Success(new AuthenticationTicket(original, "Cookies"))));
        await using var services = new ServiceCollection()
            .AddSingleton(schemes)
            .AddSingleton(authentication)
            .AddSingleton(http)
            .AddSingleton<AllowingPolicy>()
            .BuildServiceProvider();
        var runtime = new AspNetAuthorizationPolicyRuntime(new ArcAuthorizationPolicyRuntime(
            [new AuthorizationPolicyRegistration("Allowed", typeof(AllowingPolicy))]));
        var declarations = new AuthorizationDeclarations(anonymous, attributes);
        var evaluation = new AuthorizationEvaluation(declarations, legacy, accessor, runtime);
        var selected = await evaluation.Prepare(typeof(ProtectedCommand), services, CancellationToken.None);
        _selectedPrincipalWasDistinct = !ReferenceEquals(original, selected.SelectedPrincipal);
        _principalChanged = selected.PrincipalChanged;
        _allowed = await evaluation.IsAuthorized(typeof(ProtectedCommand), new ProtectedCommand(), services, CancellationToken.None);
        _policyEvaluated = services.GetRequiredService<AllowingPolicy>().WasEvaluated;
    }

    [Fact] void should_select_a_new_principal_instance() => _selectedPrincipalWasDistinct.ShouldBeTrue();
    [Fact] void should_keep_the_original_default_identity_scope() => _principalChanged.ShouldBeFalse();
    [Fact] void should_allow_the_already_checked_policy_with_the_default_evaluator() => _allowed.ShouldBeTrue();
    [Fact] void should_evaluate_the_policy() => _policyEvaluated.ShouldBeTrue();
    [Fact] void should_not_reuse_the_marker_for_a_different_principal() => _differentPrincipalFailure.ShouldBeOfExactType<AsynchronousAuthorizationRequired>();

    [Authorize(Policy = "Allowed", AuthenticationSchemes = "Cookies", Roles = "Admin")]
    public record ProtectedCommand;

    public class AllowingPolicy : IAuthorizationPolicy
    {
        public bool WasEvaluated { get; private set; }

        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            WasEvaluated = true;
            return ValueTask.FromResult(true);
        }
    }

    class DelegatingEvaluator(AuthorizationEvaluator inner, Action checkDifferentPrincipal) : IAuthorizationEvaluator
    {
        public bool IsAuthorized(Type type)
        {
            checkDifferentPrincipal();
            return inner.IsAuthorized(type);
        }

        public bool IsAuthorized(MethodInfo method) => inner.IsAuthorized(method);
    }
}
