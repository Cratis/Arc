// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_NativeUnionSymbols;

public class when_reading_cases : Specification
{
    [Theory]
    [InlineData("public class Outcome { public Outcome(string value) {} public static implicit operator Outcome(int value) => new(\"\"); }")]
    [InlineData("namespace Other { public interface IUnion {} } public class Outcome : Other.IUnion { public Outcome(string value) {} }")]
    public async Task should_ignore_user_constructors_and_unrelated_union_interfaces(string source)
    {
        var compilation = (await TestProject.CreateProject(source).GetCompilationAsync(TestContext.Current.CancellationToken))!;
        compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        NativeUnionSymbols.Cases(compilation.GetTypeByMetadataName("Outcome")!).IsDefault.ShouldBeTrue();
    }

#if NET11_0_OR_GREATER
    const string Union = "public union Outcome<T>(T, string) { public Outcome(int value, bool flag) : this(\"\") {} private Outcome(bool value) : this(\"\") {} public static implicit operator Outcome<T>(double value) => new(\"\"); }";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_preserve_only_generated_cases_and_substitute_generic_parameters(bool metadata)
    {
        var project = metadata
            ? TestProject.CreateProject("public class Consumer;", await NativeUnionSource.Emit(Union))
            : TestProject.CreateProject(Union);
        var compilation = (await project.GetCompilationAsync(TestContext.Current.CancellationToken))!;
        compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var union = compilation.GetTypeByMetadataName("Outcome`1")!.Construct(compilation.GetSpecialType(SpecialType.System_Int64));
        NativeUnionSymbols.Cases(union).Select(type => type.SpecialType).Order().ShouldEqual(
            new[] { SpecialType.System_Int64, SpecialType.System_String }.Order());
    }

    [Fact]
    public async Task should_bound_recursion_through_a_native_contract()
    {
        var compilation = (await TestProject.CreateProject(
            "public class Cycle : System.Runtime.CompilerServices.IUnion { public object Value => this; [System.Runtime.CompilerServices.CompilerGenerated] public Cycle(Cycle value) {} }").GetCompilationAsync(TestContext.Current.CancellationToken))!;
        compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        RawGuidResponseAnalysis.UnionBranches(compilation.GetTypeByMetadataName("Cycle")!, compilation).ShouldBeEmpty();
    }
#endif
}
