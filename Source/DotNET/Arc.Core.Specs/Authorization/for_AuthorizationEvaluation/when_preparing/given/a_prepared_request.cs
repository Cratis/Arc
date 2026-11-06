// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;
using Cratis.Arc.Identity.Callers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation.when_preparing.given;

public class a_prepared_request : Specification
{
    protected ClaimsPrincipal _verified;
    protected ClaimsPrincipal _selectedByScheme;
    protected AuthorizationEvaluation _evaluation;
    protected IServiceProvider _services;

    void Establish()
    {
        _verified = VerifiedCallerPrincipal.Create(Authorization.given.verified_callers.Person("person-1"));
        var request = Substitute.For<IHttpRequestContext>();
        request.Items.Returns(new Dictionary<object, object?> { [VerifiedCallerPrincipal.ItemKey] = _verified });
        var requestAccessor = Substitute.For<IHttpRequestContextAccessor>();
        requestAccessor.Current.Returns(request);
        _services = new ServiceCollection().AddSingleton(requestAccessor).BuildServiceProvider();

        var principals = Substitute.For<ICurrentPrincipalAccessor>();
        principals.Current.Returns(_verified);
        var resolution = Substitute.For<IAuthorizationPolicyResolution>();
        resolution.SelectPrincipal(Arg.Any<ClaimsPrincipal?>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<ClaimsPrincipal?>(_selectedByScheme));
        var runtime = Substitute.For<IAuthorizationPolicyRuntime>();
        runtime.Resolve(Arg.Any<IReadOnlyList<AuthorizationRequirement>>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(resolution));
        _evaluation = new AuthorizationEvaluation(Authorization.given.verified_callers.Declarations(), Substitute.For<IAuthorizationEvaluator>(), principals, runtime);
    }

    protected async Task<ClaimsPrincipal?> PrepareSelectedPrincipal() =>
        (await _evaluation.Prepare(typeof(Target), _services, CancellationToken.None)).SelectedPrincipal;

    public class Target;
}
