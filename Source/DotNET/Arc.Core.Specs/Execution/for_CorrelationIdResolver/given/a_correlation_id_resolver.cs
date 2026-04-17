// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Execution.for_CorrelationIdResolver.given;

public class a_correlation_id_resolver : Specification
{
    protected ICorrelationIdAccessor _correlationIdAccessor;
    protected CorrelationId _currentCorrelationId;

    void Establish()
    {
        _correlationIdAccessor = Substitute.For<ICorrelationIdAccessor>();
        _currentCorrelationId = CorrelationId.NotSet;
        _correlationIdAccessor.Current.Returns(_ => _currentCorrelationId);
    }
}
