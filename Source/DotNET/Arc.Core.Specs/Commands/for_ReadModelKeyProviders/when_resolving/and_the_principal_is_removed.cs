// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;

namespace Cratis.Arc.Commands.for_ReadModelKeyProviders.when_resolving;

public class and_the_principal_is_removed : Specification
{
    Exception _exception;
    CountingKey _provider;
    IDisposable _frame;

    void Establish()
    {
        _frame = AuthorizationCommandIdentity.Enter(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "represented")], "test")));
        _provider = (CountingKey)((ILookUpByKeyProvider)new LookUpByAttribute<CountingKey>()).Provider;
    }

    void Because() => _exception = Catch.Exception(() => ReadModelKeyProviders.Resolve(new LookUpByAttribute<CountingKey>(), new(typeof(object), typeof(object), null)));

    void Destroy() => _frame.Dispose();

    [Fact] void should_report_a_changed_caller() => ((ReadModelKeyResolutionDenied)_exception).Reason.ShouldEqual(ReadModelKeyRefusal.CallerChanged);
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
