// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_AggregateRootCommitResultExtensions.given;

public class all_dependencies : Specification
{
    protected CorrelationId _correlationId;

    void Establish()
    {
        _correlationId = Guid.NewGuid();
    }
}
