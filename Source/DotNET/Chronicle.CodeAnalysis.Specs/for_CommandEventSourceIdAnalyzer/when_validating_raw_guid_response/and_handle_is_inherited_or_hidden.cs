// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventSourceIdAnalyzer.when_validating_raw_guid_response.given.response_source;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandEventSourceIdAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventSourceIdAnalyzer.when_validating_raw_guid_response;

public class and_handle_is_inherited_or_hidden
{
    [Fact]
    public async Task should_substitute_inherited_generic_event_types_and_report_on_the_command() =>
        await VerifyCS.VerifyAnalyzerAsync(
            Preamble + @"
            public record Base<T> where T : new() { public (Guid, T) Handle() => (Guid.NewGuid(), new T()); }
            [Command] public record {|#0:C|} : Base<E>;
            ",
            Warning());

    [Fact]
    public async Task should_ignore_a_hidden_base_handler() =>
        await VerifyCS.VerifyAnalyzerAsync(Preamble + @"
            public record Base { public (Guid, E) Handle() => (Guid.NewGuid(), new E()); }
            [Command] public record C : Base { public new E Handle() => new E(); }
            ");

    [Fact]
    public async Task should_respect_an_inherited_identity_provider() =>
        await VerifyCS.VerifyAnalyzerAsync(Preamble + @"
            public record Base : ICanProvideEventSourceId { public EventSourceId GetEventSourceId() => EventSourceId.New(); }
            [Command] public record C : Base { public (Guid, E) Handle() => (Guid.NewGuid(), new E()); }
            ");

    [Fact]
    public async Task should_not_treat_a_similarly_named_key_attribute_as_a_runtime_key() =>
        await VerifyCS.VerifyAnalyzerAsync(
            Preamble + @"
            namespace Other { public class KeyAttribute : Attribute; }
            [Command] public record C([Other.Key] Guid Id) { public {|#0:(Guid, E)|} Handle() => (Guid.NewGuid(), new E()); }
            ",
            Warning());
}
