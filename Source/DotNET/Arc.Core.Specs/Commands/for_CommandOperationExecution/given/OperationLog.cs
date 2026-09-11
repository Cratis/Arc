// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandOperationExecution.given;

public class OperationLog
{
    public List<string> Calls { get; } = [];
    public List<CancellationToken> ForwardTokens { get; } = [];
    public List<CancellationToken> RecoveryTokens { get; } = [];
    public List<CommandOperationFailure> Failures { get; } = [];
    public string FailOn { get; set; }
    public string FailCompensationOn { get; set; }
    public string WaitForCleanupCancellationOn { get; set; }
    public CancellationTokenSource CancelAfterExecution { get; set; }

    public async Task Execute(string name, CancellationToken cancellationToken)
    {
        Calls.Add($"execute:{name}");
        ForwardTokens.Add(cancellationToken);
        if (CancelAfterExecution is not null)
        {
            await CancelAfterExecution.CancelAsync();
        }
        if (name == FailOn)
        {
            throw new InvalidCommandOperation($"original:{name}");
        }
    }

    public async ValueTask Compensate(string name, CommandOperationFailure failure, CancellationToken cancellationToken)
    {
        Calls.Add($"compensate:{name}");
        RecoveryTokens.Add(cancellationToken);
        Failures.Add(failure);
        if (name == FailCompensationOn)
        {
            throw new InvalidCommandOperation($"recovery:{name}");
        }

        if (name == WaitForCleanupCancellationOn)
        {
            var expired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await using var registration = cancellationToken.Register(expired.SetResult);
            await expired.Task;
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
