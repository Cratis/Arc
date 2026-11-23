// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using static Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventSourceIdAnalyzer.when_validating_raw_guid_response.given.response_source;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.CodeFixVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandEventSourceIdAnalyzer, Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes.UseEventSourceIdForRawGuidResponseCodeFixProvider>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_UseEventSourceIdForRawGuidResponseCodeFixProvider;

public class when_returning_native_union_events : Specification
{
    [Fact]
    public Task should_withhold_an_unverified_union_contract_change() => VerifyCS.VerifyNoCodeFixAsync(
        Command("Outcome", "=> (Guid.NewGuid(), new E());") + "public union Outcome((Guid, E), Failure);", Warning());
}
#endif
