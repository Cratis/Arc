// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Testing.for_CommandScenario.when_constructing;

/// <summary>
/// The scenario must stay lightweight: logging resolves as a no-op, so no log sink — and no sink's
/// background infrastructure — is registered unless a spec opts in.
/// </summary>
public class with_default_services : Specification
{
    CommandScenario<PerformWork> _scenario;

    void Because() => _scenario = new CommandScenario<PerformWork>();

    [Fact] void should_not_register_any_log_sink() => _scenario.Services.Any(_ => _.ServiceType == typeof(ILoggerProvider)).ShouldBeFalse();
    [Fact] void should_register_logging() => _scenario.Services.Any(_ => _.ServiceType == typeof(ILoggerFactory)).ShouldBeTrue();

    void Destroy() => _scenario.Dispose();
}
