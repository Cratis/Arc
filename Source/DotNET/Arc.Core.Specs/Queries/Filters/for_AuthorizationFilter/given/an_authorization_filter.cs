// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Execution;

namespace Cratis.Arc.Queries.Filters.for_AuthorizationFilter.given;

public class an_authorization_filter : Specification
{
    protected IAuthorizationEvaluator _authorizationEvaluator;
    protected IQueryPerformerProviders _queryPerformerProviders;
    protected AuthorizationFilter _filter;
    protected QueryContext _context;
    protected CorrelationId _correlationId;
    protected IQueryPerformer _queryPerformer;

    void Establish()
    {
        _correlationId = CorrelationId.New();
        _authorizationEvaluator = Substitute.For<IAuthorizationEvaluator>();
        _queryPerformerProviders = Substitute.For<IQueryPerformerProviders>();
        _queryPerformer = Substitute.For<IQueryPerformer>();
        _filter = new AuthorizationFilter(_authorizationEvaluator, _queryPerformerProviders);
    }
}