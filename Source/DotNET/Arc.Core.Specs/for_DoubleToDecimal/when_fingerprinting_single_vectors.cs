// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.for_DoubleToDecimal.given;

namespace Cratis.Arc.for_DoubleToDecimal;

public class when_fingerprinting_single_vectors
{
    /// <summary>Computed with Convert.ToDecimal on .NET 10, so every runtime is held to the .NET 10 result.</summary>
    const string Net10Fingerprint = "10BD8996084148D9B617AC9D443BDDBE98BD93A61711D4D8DE862F8F9ACE6E35";

    [Fact]
    public void should_match_the_dotnet_10_fingerprint() =>
        floating_point_vectors.Fingerprint(floating_point_vectors.Singles.Select(value => (Func<decimal>)(() => SingleToDecimal.Convert(value)))).ShouldEqual(Net10Fingerprint);
}
