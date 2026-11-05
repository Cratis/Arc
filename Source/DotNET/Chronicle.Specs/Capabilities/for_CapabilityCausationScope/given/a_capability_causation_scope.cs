// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Capabilities;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Auditing;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Capabilities.for_CapabilityCausationScope.given;

public class a_capability_causation_scope : Specification
{
    private protected CapabilityCausationScope _scope;
    protected CausationManager _manager;
    protected IServiceProvider _services;
    protected ICapabilityCallers _callers;
    protected CommandContext _context;

    void Establish()
    {
        _scope = new();
        _manager = new();
        _services = Substitute.For<IServiceProvider>();
        _callers = Substitute.For<ICapabilityCallers>();
        _callers.Current.Returns(new CapabilityCaller("capabilities", "rest", CapabilityPrincipalKind.Person, "person", null, null, "credential", null, null, new HashSet<string>()));
        _services.GetService(typeof(ICapabilityCallers)).Returns(_callers);
        _services.GetService(typeof(ICausationManager)).Returns(_manager);
        _context = new(CorrelationId.New(), typeof(ProbeCommand), new ProbeCommand(), [], new(), ServiceProvider: _services);
    }

    protected record ProbeCommand;
}
