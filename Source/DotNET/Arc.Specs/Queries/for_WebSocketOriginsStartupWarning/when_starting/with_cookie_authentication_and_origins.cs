// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_WebSocketOriginsStartupWarning.when_starting;

public class with_cookie_authentication_and_origins : given.a_startup_warning
{
    void Establish()
    {
        _schemes.Add(Cookies());
        _options.Query.AllowedWebSocketOrigins = ["https://app.test"];
    }

    Task Because() => Start();

    [Fact] void should_not_warn() => _levels.Count(_ => _ == LogLevel.Warning).ShouldEqual(0);
}
