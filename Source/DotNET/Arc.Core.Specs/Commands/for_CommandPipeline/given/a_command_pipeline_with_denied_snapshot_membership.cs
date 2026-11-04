// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.Arc.Tenancy;
using Cratis.Arc.Validation;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Commands.for_CommandPipeline.given;

public class a_command_pipeline_with_denied_snapshot_membership : a_command_pipeline_and_a_handler_for_command
{
    protected class ReadingValidator : AbstractValidator<string>
    {
        public ReadingValidator() => RuleFor(_ => _).Custom((_, context) => context.GetTenant());
    }

    protected ValidatorInvoker _invoker;
    IDisposable _boundary;

    void Establish()
    {
        _boundary = TenantIdAccessor.Independent.BeginRequest();
        var resolver = Substitute.For<ITenantIdResolver>();
        resolver.Resolve().Returns("selected");
        var membership = Substitute.For<ITenantMembership>();
        membership.IsMember(Arg.Any<ClaimsPrincipal>(), Arg.Any<TenantId>()).Returns(false);
        var requests = Substitute.For<IHttpRequestContextAccessor>();
        var request = Substitute.For<IHttpRequestContext>();
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IEnumerable<ITenantMembership>)).Returns(new[] { membership });
        request.RequestServices.Returns(services);
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user-1")], "test"));
        request.User.Returns(user);
        requests.Current.Returns(request);
        var principals = new CurrentPrincipalAccessor(requests);
        var verification = new TenantMembershipVerification(requests, principals, NullLogger<TenantMembershipVerification>.Instance);
        var accessor = new TenantIdAccessor(resolver, verification);
        _serviceProvider.GetService(typeof(ICurrentPrincipalAccessor)).Returns(principals);
        _serviceProvider.GetService(typeof(TenantIdAccessor)).Returns(accessor);
        _serviceProvider.GetService(typeof(ITenantIdAccessor)).Returns(accessor);
        _serviceProvider.GetService(typeof(ITenantIdResolver)).Returns(resolver);
        _invoker = new ValidatorInvoker(NullLogger<ValidatorInvoker>.Instance);
    }

    void Destroy() => _boundary.Dispose();
}
