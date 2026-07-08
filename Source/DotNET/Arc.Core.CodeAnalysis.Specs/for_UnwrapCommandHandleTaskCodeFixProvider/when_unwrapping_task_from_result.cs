// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.CodeFixVerifier<Cratis.Arc.CodeAnalysis.CommandHandleTaskWrappingAnalyzer, Cratis.Arc.CodeAnalysis.CodeFixes.UnwrapCommandHandleTaskCodeFixProvider>;

namespace Cratis.Arc.CodeAnalysis.for_UnwrapCommandHandleTaskCodeFixProvider;

public class when_unwrapping_task_from_result
{
    const string Source = @"
using System.Threading.Tasks;
using Cratis.Arc.Commands.ModelBound;

namespace TestNamespace
{
    [Command]
    public record RegisterAuthor(string Name)
    {
        public Task<string> {|#0:Handle|}() => Task.FromResult(Name);
    }
}";

    const string FixedSource = @"
using System.Threading.Tasks;
using Cratis.Arc.Commands.ModelBound;

namespace TestNamespace
{
    [Command]
    public record RegisterAuthor(string Name)
    {
        public string Handle() => Name;
    }
}";

    [Fact] async Task should_unwrap_to_synchronous_signature() => await VerifyCS.VerifyCodeFixAsync(
        Source,
        FixedSource,
        new ExpectedDiagnostic("ARC0010", DiagnosticSeverity.Warning, "RegisterAuthor"));
}
