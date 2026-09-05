// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_HostBuilderExtensions.when_adding_cratis_arc_core;

public class it_should_register_cancellation_aware_command_pipeline : Specification
{
    ICommandPipeline _pipeline;
    ICommandPipelineWithCancellation _cancellationAwarePipeline;

    void Because()
    {
        using var serviceProvider = new ServiceCollection()
            .AddCratisArcCore()
            .BuildServiceProvider();

        _pipeline = serviceProvider.GetRequiredService<ICommandPipeline>();
        _cancellationAwarePipeline = serviceProvider.GetRequiredService<ICommandPipelineWithCancellation>();
    }

    [Fact] void should_register_the_cancellation_aware_pipeline() => _cancellationAwarePipeline.ShouldNotBeNull();
    [Fact] void should_use_the_same_pipeline_instance() => ReferenceEquals(_pipeline, _cancellationAwarePipeline).ShouldBeTrue();
}
