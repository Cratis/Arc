// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Workload;

public class when_workload_hosting_has_no_capability_boundary : Specification
{
    Exception? _error;

    async Task Because()
    {
        await using var services = new ServiceCollection().BuildServiceProvider();
        _error = await Record.ExceptionAsync(() => new WorkloadCallerStartupValidation(services).StartAsync(CancellationToken.None));
    }

    [Fact] void should_refuse_startup() => _error.ShouldBeOfExactType<InvalidVerifiedCallerInstallation>();
}
