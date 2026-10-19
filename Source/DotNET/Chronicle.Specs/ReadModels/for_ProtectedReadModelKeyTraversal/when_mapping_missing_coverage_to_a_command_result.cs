// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.ReadModels.for_ProtectedReadModelKeyTraversal;

public class when_mapping_missing_coverage_to_a_command_result : Specification
{
    CommandResult _result;
    void Because() => _result = CommandResult.FromException(CorrelationId.NotSet, new ProtectedReadModelKeyAccessorMissing(typeof(object)));
    [Fact] void should_report_a_configuration_error() => _result.HasExceptions.ShouldBeTrue();
    [Fact] void should_not_report_a_missing_model_validation_failure() => _result.ValidationResults.ShouldBeEmpty();
    [Fact] void should_not_report_success() => _result.IsSuccess.ShouldBeFalse();
}
