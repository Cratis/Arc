// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_TemplateConnectionResolver.when_resolving;

public class with_an_unknown_placeholder : given.a_request_for_a_tenant
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => new TemplateConnectionResolver(new() { Database = "app-{region}" }).Resolve(_request).GetAwaiter().GetResult());

    [Fact] void should_fail() => _error.ShouldBeOfExactType<UnknownConnectionPlaceholder>();
    [Fact] void should_name_the_placeholder() => _error.Message.ShouldContain("{region}");
}
