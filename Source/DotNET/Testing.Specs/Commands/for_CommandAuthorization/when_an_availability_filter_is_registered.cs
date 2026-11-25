// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_an_availability_filter_is_registered : given.an_evaluation
{
    void Establish() => Services.AddSingleton<IInstancesOf<ICommandFilter>>(new KnownInstancesOf<ICommandFilter>([Substitute.For<IAvailabilityCommandFilter>()]));
    async Task Because() => Result = await CommandAuthorization.For<given.RoleCommand>(Provider, Principal);
    [Fact] void should_be_unsupported() => Result.ShouldBeUnsupported();
    [Fact] void should_identify_the_filter() => Result.ReasonCode.ShouldEqual("custom-filter");
}
