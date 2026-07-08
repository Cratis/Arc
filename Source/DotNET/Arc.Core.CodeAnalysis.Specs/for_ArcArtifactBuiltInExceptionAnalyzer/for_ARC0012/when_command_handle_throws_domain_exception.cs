// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.ArcArtifactBuiltInExceptionAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_ArcArtifactBuiltInExceptionAnalyzer.for_ARC0012;

public class when_command_handle_throws_domain_exception
{
    [Fact] async Task should_not_report_diagnostic() => await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using Cratis.Arc.Commands.ModelBound;

namespace TestNamespace
{
    public class AuthorAlreadyRegistered : Exception
    {
    }

    [Command]
    public record RegisterAuthor(string Name)
    {
        public void Handle()
        {
            throw new AuthorAlreadyRegistered();
        }
    }
}");
}
