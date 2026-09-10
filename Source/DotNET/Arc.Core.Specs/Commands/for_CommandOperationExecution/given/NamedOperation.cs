// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandOperationExecution.given;

public record NamedOperation(string Name) : ICommandOperation
{
    public Task Execute(OperationLog log, CancellationToken cancellationToken) => log.Execute(Name, cancellationToken);
    public ValueTask Compensate(OperationLog log, CommandOperationFailure failure, CancellationToken cancellationToken) => log.Compensate(Name, failure, cancellationToken);
}
