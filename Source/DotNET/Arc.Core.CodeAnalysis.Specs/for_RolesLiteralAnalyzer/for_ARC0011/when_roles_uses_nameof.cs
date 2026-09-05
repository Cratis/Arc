// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.RolesLiteralAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_RolesLiteralAnalyzer.for_ARC0011;

public class when_roles_uses_nameof
{
    [Fact] async Task should_not_report_diagnostic() => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Authorization;

namespace TestNamespace
{
    public enum Role { Admin }

    [Roles(nameof(Role.Admin))]
    public class SomeController
    {
    }
}");
}
