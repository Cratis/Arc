// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;

namespace Cratis.Arc.MongoDB.for_MongoDBConnectionResolutionHooks.given;

public class hooks : Specification
{
    protected ICachingConnectionResolver _resolver;
    protected ConnectionRequest _request;
    protected MongoDBConnectionResolutionHooks _hooks;

    void Establish()
    {
        _resolver = Substitute.For<ICachingConnectionResolver>();
        _request = new(WellKnownConnectionStorage.MongoDB, ConnectionPurpose.Runtime, new Dictionary<string, string> { ["tenant"] = "acme" });
        _hooks = new(_resolver, _request, TimeProvider.System);
    }
}
