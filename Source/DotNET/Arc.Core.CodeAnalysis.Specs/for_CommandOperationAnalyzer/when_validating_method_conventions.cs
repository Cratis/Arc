// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Verify = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.CommandOperationAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_CommandOperationAnalyzer;

public class when_validating_method_conventions
{
    [Theory]
    [InlineData("public int Execute() => 1;")]
    [InlineData("public static void Execute() { }")]
    [InlineData("public void Execute<T>() { }")]
    [InlineData("public void Execute(ref int value) { }")]
    [InlineData("public void Execute(params string[] values) { }")]
    [InlineData("public void Execute(System.IServiceProvider provider) { }")]
    [InlineData("public void Execute(CommandOperationFailure failure) { }")]
    [InlineData("public async void Execute() { await System.Threading.Tasks.Task.Yield(); }")]
    [InlineData("public void Execute() { } public void Execute(string value) { }")]
    [InlineData("public void Execute() { } private void Compensate() { }")]
    public async Task should_reject_invalid_methods(string methods) => await Verify.VerifyAnalyzerAsync(
        "using Cratis.Arc.Commands; public record {|#0:Invalid|} : ICommandOperation { " + methods + " }",
        Verify.Diagnostic("ARC0016").WithLocation(0));

    [Fact] public async Task should_accept_task_and_value_task_with_failure_context() => await Verify.VerifyAnalyzerAsync(@"
        using Cratis.Arc.Commands;
        using System.Threading;
        using System.Threading.Tasks;
        public interface IReservations { }
        public record Valid : ICommandOperation
        {
            public Task Execute(IReservations reservations, CancellationToken token) => Task.CompletedTask;
            public ValueTask Compensate(IReservations reservations, CommandOperationFailure failure, CancellationToken token) => ValueTask.CompletedTask;
        }");
}
