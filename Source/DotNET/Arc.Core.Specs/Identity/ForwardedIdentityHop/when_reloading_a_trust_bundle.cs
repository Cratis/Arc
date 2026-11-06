// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.for_ForwardedIdentityHop.given;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Identity.for_ForwardedIdentityHop;

public class when_reloading_a_trust_bundle : Specification
{
    long _initial;
    long _unchanged;
    long _changed;
    long _bad;
    long _oversized;
    bool _initialOversize;
    int _warnings;

    void Because()
    {
        using var first = new certificates();
        using var second = new certificates();
        var clock = new Clock();
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Warning).Returns(true);
        var trust = ForwardedIdentityClientCertificateTrust.For(first.Bundle, clock);
        trust.Load(TimeSpan.FromSeconds(5), logger);
        _initial = trust.Generation;
        clock.Advance();
        trust.Load(TimeSpan.FromSeconds(5), logger);
        _unchanged = trust.Generation;
        File.WriteAllText(first.Bundle, second.Root.ExportCertificatePem());
        clock.Advance();
        trust.Load(TimeSpan.FromSeconds(5), logger);
        _changed = trust.Generation;
        File.WriteAllText(first.Bundle, "malformed replacement");
        clock.Advance();
        trust.Load(TimeSpan.FromSeconds(5), logger);
        _bad = trust.Generation;
        File.WriteAllText(first.Bundle, new string('x', (1024 * 1024) + 1));
        clock.Advance();
        trust.Load(TimeSpan.FromSeconds(5), logger);
        _oversized = trust.Generation;
        File.WriteAllText(second.Bundle, new string('x', (1024 * 1024) + 1));
        _initialOversize = ForwardedIdentityClientCertificateTrust.For(second.Bundle, clock).Load(TimeSpan.FromSeconds(5), logger);
        _warnings = logger.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(ILogger.Log));
    }

    [Fact] void should_load_an_initial_generation() => _initial.ShouldEqual(1L);
    [Fact] void should_not_advance_for_unchanged_content() => _unchanged.ShouldEqual(_initial);
    [Fact] void should_advance_for_valid_changed_content() => _changed.ShouldEqual(_initial + 1);
    [Fact] void should_keep_the_last_good_generation_on_malformed_content() => _bad.ShouldEqual(_changed);
    [Fact] void should_keep_the_last_good_generation_on_oversized_content() => _oversized.ShouldEqual(_changed);
    [Fact] void should_refuse_an_oversized_initial_bundle() => _initialOversize.ShouldBeFalse();
    [Fact] void should_warn_for_each_invalid_replacement() => _warnings.ShouldEqual(3);

    sealed class Clock : TimeProvider
    {
        DateTimeOffset _now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => _now;
        internal void Advance() => _now += TimeSpan.FromSeconds(5);
    }
}
