// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_ReadProtectionMsBuild.when_emitting;

public class with_lower_case_on : given.a_read_protection_build
{
    void Establish()
    {
        _buildMode = "on";
        _expectFailure = true;
    }

    Task Because() => GenerateAndResolve();
    [Fact] void should_fail_the_build() => _exitCode.ShouldNotEqual(0);
    [Fact] void should_explain_the_supported_values() => _output.ShouldContain("CratisReadProtection must be Off, Measure, or On.");
}
