// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_using_certified_validator_construction;

public class with_cloned_pipelines : given.a_certified_command
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_refuse_on_execute_and_validate_from_both_pipelines_even_with_a_severity_override(bool factoryOverride)
    {
        Build();
        IServiceCollection copied = new ServiceCollection();
        foreach (var descriptor in _scenario.Services) copied.Add(descriptor);
        var calls = 0;
        if (factoryOverride)
        {
            copied.AddTransient<CertifiedValidator>(_ =>
            {
                calls++;
                throw new InvalidOperationException("cloned factory");
            });
        }
        await using var clone = copied.BuildServiceProvider();
        await using var scope = clone.CreateAsyncScope();
        foreach (var pipeline in new[] { _pipeline, clone.GetRequiredService<ICommandPipeline>() })
        {
            var validation = await pipeline.Validate(_command, scope.ServiceProvider, ValidationResultSeverity.Error);
            var execution = await pipeline.Execute(_command, scope.ServiceProvider, ValidationResultSeverity.Error);
            validation.ExceptionMessages.Single().ShouldContain("Discoverable validator");
            execution.ExceptionMessages.Single().ShouldContain("Discoverable validator");
        }
        calls.ShouldEqual(0);
        _scenario.AppendedEvents.ShouldBeEmpty();
    }
}
