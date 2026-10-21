// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_TemplateConnectionResolver.given;

public class a_request_for_a_tenant : Specification
{
    protected ConnectionRequest _request;

    void Establish() => _request = new(
        WellKnownConnectionStorage.SqlServer,
        ConnectionPurpose.Schema,
        new Dictionary<string, string> { [WellKnownConnectionScopeKeys.Tenant] = "acme" });
}
