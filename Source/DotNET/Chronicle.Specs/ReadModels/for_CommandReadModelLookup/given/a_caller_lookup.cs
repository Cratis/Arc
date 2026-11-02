// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandReadModelLookup.given;

public class a_caller_lookup : a_lookup
{
    protected IDisposable _frame;
    protected ReadModelKey[] _keys;
    protected ClaimsPrincipal _principal;
    protected ICurrentPrincipalAccessor _principalAccessor;
    private protected ProtectedReadModelKeyTraversal _traversal;
    protected Exception?[] _exceptions;

    void Establish()
    {
        Configure(new Command());
        _principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "selected")], "test"));
        _principalAccessor = Substitute.For<ICurrentPrincipalAccessor>();
        _principalAccessor.Current.Returns(_ => _principal);
        _context = _context with { ServiceProvider = new CallerServices(_context.ServiceProvider, _principalAccessor) };
        _traversal = new(Options.Create(new ProtectedReadModelKeyOptions()), NullLogger<ProtectedReadModelKeyTraversal>.Instance);
        _frame = AuthorizationCommandIdentity.Enter(_principal);
    }

    protected void Resolve<TCommand>() => _keys = Enum.GetValues<ReadProtectionMode>().Select(mode => CommandReadModelLookup.Resolve(_context, typeof(TCommand).GetMethod("Handle").GetParameters()[0], mode, _traversal, out _)).ToArray();

    protected void Refuse<TCommand>() => _exceptions = Enum.GetValues<ReadProtectionMode>().Select(mode => Catch.Exception(() => CommandReadModelLookup.Resolve(_context, typeof(TCommand).GetMethod("Handle").GetParameters()[0], mode))).ToArray();

    void Destroy() => _frame.Dispose();

    sealed class CallerServices(IServiceProvider services, ICurrentPrincipalAccessor accessor) : IServiceProvider
    {
        public object? GetService(Type serviceType) => serviceType == typeof(ICurrentPrincipalAccessor) ? accessor : services.GetService(serviceType);
    }

    protected class Command
    {
        public string Id { get; set; } = "client-key";
        public void Handle([LookUpBy<CallerSubject>] Model model) { }
    }

    protected record Model(string Id);
}
