// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.ArcArtifactBuiltInExceptionAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_ArcArtifactBuiltInExceptionAnalyzer.for_ARC0012;

public class when_unrelated_class_throws_built_in_exception
{
    [Fact] async Task should_not_report_diagnostic() => await VerifyCS.VerifyAnalyzerAsync(@"
using System;

namespace TestNamespace
{
    public class SomeService
    {
        public void DoWork()
        {
            throw new InvalidOperationException(""not an arc artifact"");
        }
    }
}");
}
