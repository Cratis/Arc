// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventSourceIdAnalyzer.when_validating_raw_guid_response.given.response_source;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandEventSourceIdAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventSourceIdAnalyzer.when_validating_raw_guid_response;

public class and_response_uses_supported_wrappers
{
    [Theory]
    [InlineData("Task<(Guid, E)>", "=> Task.FromResult((Guid.NewGuid(), new E()));")]
    [InlineData("ValueTask<(Guid, E)>", "=> new((Guid.NewGuid(), new E()));")]
    [InlineData("Result<(Guid, E), Failure>", "=> (Guid.NewGuid(), new E());")]
    [InlineData("OneOf<(Guid, E), Failure>", "=> (Guid.NewGuid(), new E());")]
    [InlineData("Task<Result<(Guid, E), Failure>>", "=> Task.FromResult<Result<(Guid, E), Failure>>((Guid.NewGuid(), new E()));")]
    [InlineData("(Guid, OneOf<E, Failure>)", "=> (Guid.NewGuid(), new E());")]
    [InlineData("(Guid, Result<E, Failure>)", "=> (Guid.NewGuid(), new E());")]
    [InlineData("(Guid, E[])", "=> (Guid.NewGuid(), new[] { new E() });")]
    [InlineData("(Guid, IEnumerable<E>)", "=> (Guid.NewGuid(), new[] { new E() });")]
    [InlineData("(Guid, List<E>)", "=> (Guid.NewGuid(), new List<E> { new E() });")]
    [InlineData("(Guid, E, EventForEventSourceId)", "=> (Guid.NewGuid(), new E(), new EventForEventSourceId(EventSourceId.New(), new E()));")]
    [InlineData("(Guid, OneOf<EventForEventSourceId, E>)", "=> (Guid.NewGuid(), new E());")]
    public async Task should_warn_about_the_bare_event_branch(string signature, string body) =>
        await VerifyCS.VerifyAnalyzerAsync(Command(signature, body), Warning());
}
