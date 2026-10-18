// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Chronicle.ReadModels.for_ProtectedReadModelKeyTraversal;

public class when_using_the_jit_fallback : given.a_traversal
{
    static readonly RecordingLogger _processLogger = new();
    static readonly bool _valid;
    static readonly bool _invalid;

    /// <summary>
    /// Uses one exact type once per process even under the per-fact lifecycle, just like real fallback warnings.
    /// </summary>
    static when_using_the_jit_fallback()
    {
        var traversal = new ProtectedReadModelKeyTraversal(Options.Create(new ProtectedReadModelKeyOptions()), _processLogger) { DynamicCodeSupported = true };
        _valid = traversal.IsValid(new ExternalShape("secret-valid"), "wire");
        _invalid = traversal.IsValid(new ExternalShape("secret#invalid"), "wire");
    }

    void Because() { }

    [Fact] void should_read_valid_public_properties() => _valid.ShouldBeTrue();
    [Fact] void should_reject_invalid_public_properties() => _invalid.ShouldBeFalse();
    [Fact] void should_warn_once_per_exact_type() => _processLogger.Entries.Count.ShouldEqual(1);
    [Fact] void should_log_at_warning_level() => _processLogger.Entries.Single().Level.ShouldEqual(LogLevel.Warning);
    [Fact] void should_name_the_type() => _processLogger.Entries.Single().Text.ShouldContain(typeof(ExternalShape).FullName!);
    [Fact] void should_point_to_registration() => _processLogger.Entries.Single().Text.ShouldContain("ProtectedReadModelKeyAccessors.Register<T>()");
    [Fact] void should_point_to_strict_mode() => _processLogger.Entries.Single().Text.ShouldContain("ProtectedReadModelKeyOptions.Strict");
    [Fact] void should_never_log_key_values() => _processLogger.Entries.Single().Text.ShouldNotContain("secret");

    public record ExternalShape(string Part)
    {
        public string this[int index] => "*";
    }
}
