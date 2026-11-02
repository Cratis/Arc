// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;

namespace Cratis.Arc.Commands.for_ReadModelKeyProviders;

public class when_the_caller_changed : Specification
{
    Exception _exception;
    CountingKey _provider;

    void Because()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "represented")], "test"));
        using var frame = AuthorizationCommandIdentity.Enter(principal);
        ILookUpByKeyProvider attribute = new LookUpByAttribute<CountingKey>();
        _provider = (CountingKey)attribute.Provider;
        ((ClaimsIdentity)principal.Identity).AddClaim(new Claim("sub", "later"));
        _exception = Catch.Exception(() => ReadModelKeyProviders.Resolve(attribute, typeof(object), typeof(object), principal));
    }

    [Fact] void should_deny_resolution() => ((ReadModelKeyResolutionDenied)_exception).Reason.ShouldEqual(ReadModelKeyRefusal.CallerChanged);
    [Fact] void should_not_invoke_the_provider() => _provider.Calls.ShouldEqual(0);

    public sealed class CountingKey : IReadModelKeyProvider
    {
        public int Calls { get; private set; }
        public ReadModelKeyLookup Resolve(ReadModelKeyContext context)
        {
            Calls++;
            return ReadModelKeyLookup.Found("key");
        }
    }
}
