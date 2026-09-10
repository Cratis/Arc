// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Commands.for_CommandOperationExecution.given;

[Command]
public record ScopedProbeCommand
{
    public ScopedProbeOperation Handle() => new();
}
