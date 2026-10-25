// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup;

public class when_registered_more_than_once : Specification
{
    ServiceCollection _services;

    void Establish() => _services = [];

    void Because()
    {
        _services.AddCommandAwareDecisionReads();
        _services.AddCommandAwareDecisionReads();
    }

    [Fact] void should_register_one_hosted_preflight() => _services.Count(descriptor => descriptor.ServiceType == typeof(IHostedService) && descriptor.ImplementationType == typeof(DecisionReadAdmissionStartup)).ShouldEqual(1);
    [Fact] void should_register_one_reader_factory() => _services.Count(descriptor => descriptor.ServiceType == typeof(IStartupDecisionReads)).ShouldEqual(1);
}
