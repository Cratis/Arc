// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.for_DoubleToDecimal.given;

namespace Cratis.Arc.for_DoubleToDecimal;

public class when_fingerprinting_double_vectors
{
    /// <summary>Computed with Convert.ToDecimal on .NET 10, so every runtime is held to the .NET 10 result.</summary>
    const string Net10Fingerprint = "8D13A87BB72812D78102BCEA7C3885FA0AE60C2EABB251B6D9C3120EA18474AF";

    [Fact]
    public void should_match_the_dotnet_10_fingerprint() =>
        floating_point_vectors.Fingerprint(floating_point_vectors.Doubles.Select(value => (Func<decimal>)(() => DoubleToDecimal.Convert(value)))).ShouldEqual(Net10Fingerprint);
}
