// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandOperationExecution.given;

public sealed class ScopedOperationProbe(List<string> calls) : IDisposable
{
    bool _disposed;

    public void Use(string phase)
    {
        if (_disposed)
        {
            throw new InvalidCommandOperation("The scoped dependency was disposed before recovery.");
        }

        calls.Add(phase);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            calls.Add("dispose");
        }

        GC.SuppressFinalize(this);
    }
}
