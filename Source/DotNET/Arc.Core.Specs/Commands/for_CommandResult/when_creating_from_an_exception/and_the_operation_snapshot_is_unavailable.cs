// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandResult.when_creating_from_an_exception;

public class and_the_operation_snapshot_is_unavailable : Specification
{
    CommandResult[] _results;

    void Because()
    {
        var correlationId = CorrelationId.New();
        Exception[] exceptions = [new OperationSnapshotUnavailable(), new AggregateException(new InvalidOperationException("ordinary"), new OperationSnapshotUnavailable())];
        _results = exceptions.SelectMany(exception => new[] { CommandResult.FromException(correlationId, exception), CommandResult.Error(correlationId, exception) }).ToArray();
    }

    [Fact] void should_be_unauthorized() => _results.ShouldEachConformTo(result => !result.IsAuthorized);
    [Fact] void should_not_expose_exception_details() => _results.ShouldEachConformTo(result => !result.HasExceptions && result.ExceptionStackTrace.Length == 0);
}
