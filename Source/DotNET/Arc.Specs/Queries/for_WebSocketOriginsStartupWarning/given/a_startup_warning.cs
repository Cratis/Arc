// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries.for_WebSocketOriginsStartupWarning.given;

public class a_startup_warning : Specification
{
    protected ArcOptions _options;
    protected List<AuthenticationScheme> _schemes;
    protected List<LogLevel> _levels;
    IAuthenticationSchemeProvider _provider;

    void Establish()
    {
        _options = new();
        _schemes = [];
        _levels = [];
        _provider = Substitute.For<IAuthenticationSchemeProvider>();
        _provider.GetAllSchemesAsync().Returns(_ => Task.FromResult<IEnumerable<AuthenticationScheme>>(_schemes));
    }

    protected static AuthenticationScheme Cookies() => new("Cookies", null, typeof(CookieAuthenticationHandler));

    protected Task Start() => new WebSocketOriginsStartupWarning(Options.Create(_options), _provider, new recording_logger(_levels)).StartAsync(CancellationToken.None);

    class recording_logger(List<LogLevel> levels) : ILogger<WebSocketOriginsStartupWarning>
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            levels.Add(logLevel);
    }
}
