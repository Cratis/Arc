// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Execution;

namespace Cratis.Arc.Commands.Filters.for_AuthorizationFilter.given;

public class an_authorization_filter : Specification
{
    protected IAuthorizationEvaluator _authorizationHelper;
    protected AuthorizationFilter _filter;
    protected CommandContext _context;
    protected CorrelationId _correlationId;

    void Establish()
    {
        _correlationId = CorrelationId.New();
        _authorizationHelper = Substitute.For<IAuthorizationEvaluator>();
        _filter = new AuthorizationFilter(_authorizationHelper);
    }
}