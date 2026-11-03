// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_WebSocketOriginsStartupWarning.when_starting;

public class with_cookie_authentication_and_no_origins : given.a_startup_warning
{
    void Establish()
    {
        _schemes.Add(Cookies());
    }

    Task Because() => Start();

    [Fact] void should_log_one_warning() => _levels.Count(_ => _ == LogLevel.Warning).ShouldEqual(1);
}
