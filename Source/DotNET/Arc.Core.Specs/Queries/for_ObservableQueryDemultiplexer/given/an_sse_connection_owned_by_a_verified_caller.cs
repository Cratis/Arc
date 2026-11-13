// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization.given;
using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.given;

public class an_sse_connection_owned_by_a_verified_caller : an_sse_connection_owned_by_a_caller
{
    void Establish()
    {
        _owner = VerifiedCallerPrincipal.Create(verified_callers.Person("owner"));
        _anotherCaller = VerifiedCallerPrincipal.Create(verified_callers.Person("another"));
        _connectionContext.User.Returns(_owner);
        _connectionContext.Items.Returns(new Dictionary<object, object?> { [VerifiedCallerPrincipal.ItemKey] = _owner });
    }
}
