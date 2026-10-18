// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators.Specs.for_ModelGraphWalkerGenerator;

public class when_classifying_protected_key_types : Specification
{
    const string Models = """
        namespace Keys
        {
            public enum Choice { First }
            public abstract record AbstractKey(string Part);
            public partial record Composite(string Part);
            [Cratis.Arc.Commands.ModelBound.Command]
            public record Change(
                [Cratis.Chronicle.Keys.Key] System.Guid Guid,
                [Cratis.Chronicle.Keys.Key] System.DateOnly Date,
                [Cratis.Chronicle.Keys.Key] System.DateTime DateTime,
                [Cratis.Chronicle.Keys.Key] System.DateTimeOffset Offset,
                [Cratis.Chronicle.Keys.Key] System.TimeOnly Time,
                [Cratis.Chronicle.Keys.Key] System.TimeSpan Duration,
                [Cratis.Chronicle.Keys.Key] decimal Decimal,
                [Cratis.Chronicle.Keys.Key] Choice Enum,
                [Cratis.Chronicle.Keys.Key] System.Guid? NullableGuid,
                [Cratis.Chronicle.Keys.Key] System.DateOnly? NullableDate,
                [Cratis.Chronicle.Keys.Key] Choice? NullableEnum,
                [Cratis.Chronicle.Keys.Key] (string, System.Guid)? NullableTuple,
                [Cratis.Chronicle.Keys.Key] string Text,
                [Cratis.Chronicle.Keys.Key] bool Boolean,
                [Cratis.Chronicle.Keys.Key] char Character,
                [Cratis.Chronicle.Keys.Key] sbyte SignedByte,
                [Cratis.Chronicle.Keys.Key] byte Byte,
                [Cratis.Chronicle.Keys.Key] short Short,
                [Cratis.Chronicle.Keys.Key] ushort UnsignedShort,
                [Cratis.Chronicle.Keys.Key] int Integer,
                [Cratis.Chronicle.Keys.Key] uint UnsignedInteger,
                [Cratis.Chronicle.Keys.Key] long Long,
                [Cratis.Chronicle.Keys.Key] ulong UnsignedLong,
                [Cratis.Chronicle.Keys.Key] float Single,
                [Cratis.Chronicle.Keys.Key] double Double,
                [Cratis.Chronicle.Keys.Key] nint NativeInteger,
                [Cratis.Chronicle.Keys.Key] nuint UnsignedNativeInteger,
                [Cratis.Chronicle.Keys.Key] object RuntimeKey,
                [Cratis.Chronicle.Keys.Key] AbstractKey Abstract,
                [Cratis.Chronicle.Keys.Key] System.Version ExternalComposite,
                [Cratis.Chronicle.Keys.Key] Composite PartialComposite)
            {
                public void Handle() { }
            }
        }
        """;

    Diagnostic[] _diagnostics;

    void Because() => (_, _diagnostics) = CompositeKeyCompilation.Compile("ProtectedKeyClassification", Models);

    [Fact] void should_only_report_the_two_uncovered_composites() => _diagnostics.Length.ShouldEqual(2);
    [Fact] void should_keep_the_diagnostics_informational() => _diagnostics.All(diagnostic => diagnostic.Id == "ARC0030" && diagnostic.Severity == DiagnosticSeverity.Info).ShouldBeTrue();
    [Fact] void should_report_an_external_composite_class() => _diagnostics.ShouldContain(diagnostic => diagnostic.GetMessage().Contains("'System.Version'", StringComparison.Ordinal));
    [Fact] void should_report_a_partial_composite_record() => _diagnostics.ShouldContain(diagnostic => diagnostic.GetMessage().Contains("'Keys.Composite'", StringComparison.Ordinal));
}
