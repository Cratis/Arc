// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_HostBuilderExtensions;

public class when_registering_operation_time : Specification
{
    TimeProvider _customClock = null!;
    ServiceProvider _services = null!;

    void Establish()
    {
        _customClock = Substitute.For<TimeProvider>();
        _services = new ServiceCollection()
            .AddSingleton(_customClock)
            .AddCratisArcCore()
            .BuildServiceProvider();
    }

    [Fact] void should_preserve_an_existing_time_provider() => _services.GetRequiredService<TimeProvider>().ShouldBeSame(_customClock);
    [Fact] void should_make_the_receipt_accessible_to_validators() => _services.GetRequiredService<IOperationContextAccessor>().ShouldBeOfExactType<OperationContextAccessor>();

    void Destroy() => _services.Dispose();
}
