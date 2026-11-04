// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_handle_uses_a_caller_key : given.a_parameter_lookup
{
    void Establish() => Configure(new CallerCommand(new("to")));

    async Task Because()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "from")], "test"));
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(principal);
        _context = _context with { ServiceProvider = new CallerServices(_services, accessor) };
        using var frame = AuthorizationCommandIdentity.Enter(principal);
        await ResolveHandle();
    }

    [Fact] void should_inject_the_callers_model_not_the_client_key() => _arguments.Arguments[0].ShouldBeSame(_from);

    sealed class CallerServices(IServiceProvider services, ICurrentPrincipalAccessor accessor) : IServiceProvider
    {
        public object? GetService(Type serviceType) => serviceType == typeof(ICurrentPrincipalAccessor) ? accessor : services.GetService(serviceType);
    }

    public record CallerCommand(AccountId Id)
    {
        public void Handle([LookUpBy<CallerSubject>] Balance balance) { }
    }
}
