// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.AspNetCore.EndpointGroups.for_EndpointGroupMatcherPolicy.given;

public class candidates_on_a_port : Specification
{
    protected const int PublicPort = 5000;
    protected const int InternalPort = 5001;

    protected EndpointGroupMatcherPolicy _policy;
    protected Endpoint _defaultEndpoint;
    protected Endpoint _internalEndpoint;
    protected Endpoint _foreignEndpoint;
    protected CandidateSet _candidates;
    protected DefaultHttpContext _httpContext;

    void Establish()
    {
        var options = new ArcOptions();
        options.EndpointGroups.Assign("internal", InternalPort);
        _policy = new EndpointGroupMatcherPolicy(Options.Create(options));

        _defaultEndpoint = EndpointWith(new EndpointGroupMetadata(null));
        _internalEndpoint = EndpointWith(new EndpointGroupMetadata("internal"));
        _foreignEndpoint = EndpointWith();
        _candidates = new CandidateSet([_defaultEndpoint, _internalEndpoint, _foreignEndpoint], new RouteValueDictionary[3], [0, 0, 0]);
        _httpContext = new DefaultHttpContext();
    }

    protected bool IsValid(Endpoint endpoint)
    {
        for (var i = 0; i < _candidates.Count; i++)
        {
            if (_candidates[i].Endpoint == endpoint)
            {
                return _candidates.IsValidCandidate(i);
            }
        }

        return false;
    }

    static Endpoint EndpointWith(params object[] metadata) => new(_ => Task.CompletedTask, new EndpointMetadataCollection(metadata), null);
}
