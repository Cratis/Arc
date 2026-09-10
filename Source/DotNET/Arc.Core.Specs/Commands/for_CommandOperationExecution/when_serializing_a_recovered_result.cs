// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_serializing_a_recovered_result : given.an_operation_pipeline
{
    string _json;

    void Establish() => _log.FailOn = "private-business-key";
    async Task Because()
    {
        _result = await Run(new given.OperationCommand([new given.NamedOperation("private-business-key")]));
        _json = JsonSerializer.Serialize(_result, new ArcOptions().JsonSerializerOptions);
    }

    [Fact] void should_keep_recovery_available_to_backend_callers() => _result.Recovery.Status.ShouldEqual(CommandRecoveryStatus.Completed);
    [Fact] void should_not_add_recovery_to_http_json() => _json.Contains("recovery", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    [Fact] void should_not_serialize_invocation_outcomes() => _json.Contains("operationOutcomes", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_serialize_descriptors() => _json.Contains("operationType", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_serialize_failure_context() => _json.Contains("isFailingInvocation", StringComparison.Ordinal).ShouldBeFalse();
}
