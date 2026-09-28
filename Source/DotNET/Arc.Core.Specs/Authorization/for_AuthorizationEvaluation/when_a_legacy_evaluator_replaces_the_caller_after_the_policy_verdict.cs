// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using Cratis.Arc.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

public class when_a_legacy_evaluator_replaces_the_caller_after_the_policy_verdict : Specification
{
    bool _guestCommandAllowed;
    bool _guestQueryAllowed;
    bool _authenticatedCommandAllowed;
    bool _authenticatedQueryAllowed;
    int _delegatedVerdicts;
    int _policyEvaluations;

    async Task Because()
    {
        var anonymous = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymous.GetEnumerator().Returns(_ => new IAnonymousEvaluator[] { new AnonymousEvaluator() }.AsEnumerable().GetEnumerator());
        var attributes = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributes.GetEnumerator().Returns(_ => new IAuthorizationAttributeEvaluator[] { new AuthorizationAttributeEvaluator() }.AsEnumerable().GetEnumerator());
        var requests = Substitute.For<IHttpRequestContextAccessor>();
        var request = Substitute.For<IHttpRequestContext>();
        requests.Current.Returns(request);
        var accessor = new CurrentPrincipalAccessor(requests);
        var evaluator = new ReplacingEvaluator(new AuthorizationEvaluator(accessor, anonymous, attributes), request);
        var policy = new CallerPolicy();
        await using var services = new ServiceCollection().AddSingleton(policy).BuildServiceProvider();
        var evaluation = new AuthorizationEvaluation(
            new AuthorizationDeclarations(anonymous, attributes),
            evaluator,
            accessor,
            new ArcAuthorizationPolicyRuntime(
            [
                new AuthorizationPolicyRegistration("Guest", typeof(CallerPolicy)) { EvaluatesAnonymous = true },
                new AuthorizationPolicyRegistration("Member", typeof(CallerPolicy))
            ]));

        request.User = Guest();
        _guestCommandAllowed = await evaluation.IsAuthorized(typeof(GuestCommand), new GuestCommand(), services, CancellationToken.None);
        request.User = Guest();
        _guestQueryAllowed = await evaluation.IsAuthorized(typeof(GuestQuery).GetMethod(nameof(GuestQuery.All)), new object(), services, CancellationToken.None);
        request.User = Authenticated("evaluated");
        _authenticatedCommandAllowed = await evaluation.IsAuthorized(typeof(MemberCommand), new MemberCommand(), services, CancellationToken.None);
        request.User = Authenticated("evaluated");
        _authenticatedQueryAllowed = await evaluation.IsAuthorized(typeof(MemberQuery).GetMethod(nameof(MemberQuery.All)), new object(), services, CancellationToken.None);
        _delegatedVerdicts = evaluator.DelegatedVerdicts;
        _policyEvaluations = policy.Evaluations;
    }

    [Fact] void should_deny_the_guest_command_after_the_legacy_evaluator_authenticates_the_caller() => _guestCommandAllowed.ShouldBeFalse();
    [Fact] void should_deny_the_guest_query_after_the_legacy_evaluator_authenticates_the_caller() => _guestQueryAllowed.ShouldBeFalse();
    [Fact] void should_deny_the_member_command_after_the_legacy_evaluator_replaces_the_caller() => _authenticatedCommandAllowed.ShouldBeFalse();
    [Fact] void should_deny_the_member_query_after_the_legacy_evaluator_replaces_the_caller() => _authenticatedQueryAllowed.ShouldBeFalse();
    [Fact] void should_have_delegated_each_verdict_to_the_default_evaluator() => _delegatedVerdicts.ShouldEqual(4);
    [Fact] void should_have_evaluated_each_policy_before_the_legacy_verdict() => _policyEvaluations.ShouldEqual(4);

    static ClaimsPrincipal Guest() => new(new ClaimsIdentity());
    static ClaimsPrincipal Authenticated(string name) => new(new ClaimsIdentity([new Claim(ClaimTypes.Name, name)], "test"));

    [Authorize(Policy = "Guest")]
    public record GuestCommand;

    public static class GuestQuery
    {
        [Authorize(Policy = "Guest")]
        public static void All() { }
    }

    [Authorize(Policy = "Member")]
    public record MemberCommand;

    public static class MemberQuery
    {
        [Authorize(Policy = "Member")]
        public static void All() { }
    }

    public class CallerPolicy : IAuthorizationPolicy
    {
        public int Evaluations { get; private set; }

        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            Evaluations++;
            return ValueTask.FromResult(context.Target == typeof(GuestCommand) || context.Target == typeof(GuestQuery).GetMethod(nameof(GuestQuery.All))
                ? context.Principal.Identity?.IsAuthenticated != true
                : context.Principal.Identity?.Name == "evaluated");
        }
    }

    class ReplacingEvaluator(IAuthorizationEvaluator inner, IHttpRequestContext request) : IAuthorizationEvaluator
    {
        public int DelegatedVerdicts { get; private set; }

        public bool IsAuthorized(Type type)
        {
            var allowed = inner.IsAuthorized(type);
            DelegatedVerdicts++;
            request.User = Authenticated("unevaluated");
            return allowed;
        }

        public bool IsAuthorized(MethodInfo method)
        {
            var allowed = inner.IsAuthorized(method);
            DelegatedVerdicts++;
            request.User = Authenticated("unevaluated");
            return allowed;
        }
    }
}
