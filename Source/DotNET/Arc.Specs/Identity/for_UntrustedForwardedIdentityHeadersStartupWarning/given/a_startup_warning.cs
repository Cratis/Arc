// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity.for_UntrustedForwardedIdentityHeadersStartupWarning.given;

public class a_startup_warning : Specification
{
    protected ArcOptions _options;
    private protected UntrustedForwardedIdentityHeadersStartupWarning _warning;
    protected List<(LogLevel Level, string Message)> _messages;

    void Establish()
    {
        _options = new();
        _messages = [];
        _warning = new(Options.Create(_options), new recording_logger(_messages));
    }

    class recording_logger(List<(LogLevel Level, string Message)> messages) : ILogger<UntrustedForwardedIdentityHeadersStartupWarning>
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            messages.Add((logLevel, formatter(state, exception)));
    }
}
