// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_TemplateConnectionResolver.when_resolving;

public class without_templates : given.a_request_for_a_tenant
{
    ResolvedConnection _result;

    async Task Because() => _result = await new TemplateConnectionResolver(new()).Resolve(_request);

    [Fact] void should_leave_everything_as_configured() => _result.ShouldEqual(ResolvedConnection.Unchanged);
}
