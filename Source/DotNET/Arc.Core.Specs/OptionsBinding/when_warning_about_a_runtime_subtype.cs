// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// Without dynamic code support a subtype is still bound, with one warning per type.
/// </summary>
public class when_warning_about_a_runtime_subtype : Specification
{
    CapturingLogger _unsupported;
    CapturingLogger _supported;

    void Establish()
    {
        _unsupported = new CapturingLogger();
        _supported = new CapturingLogger();
    }

    void Because()
    {
        RuntimeTypeFallback.WarnWhenDynamicCodeIsUnavailable(_unsupported, typeof(when_warning_about_a_runtime_subtype), false);
        RuntimeTypeFallback.WarnWhenDynamicCodeIsUnavailable(_unsupported, typeof(when_warning_about_a_runtime_subtype), false);
        RuntimeTypeFallback.WarnWhenDynamicCodeIsUnavailable(_supported, typeof(CapturingLogger), true);
    }

    [Fact] void should_warn_once_for_the_type() => _unsupported.Warnings.ShouldEqual(1);

    [Fact] void should_not_warn_when_dynamic_code_is_supported() => _supported.Warnings.ShouldEqual(0);

    class CapturingLogger : ILogger
    {
        public int Warnings { get; private set; }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                Warnings++;
            }
        }
    }
}
