// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using OneOf;

namespace Cratis.Arc.ProxyGenerator.for_MethodInfoExtensions.when_getting_a_response_model;

public class and_nullable_operation_values_are_returned : Specification
{
    [Theory]
    [InlineData(nameof(Methods.NullableBatch))]
    [InlineData(nameof(Methods.NullableOperation))]
    [InlineData(nameof(Methods.TaskBatch))]
    [InlineData(nameof(Methods.ValueTaskOperation))]
    [InlineData(nameof(Methods.WrappedBatch))]
    [InlineData(nameof(Methods.WrappedOperation))]
    public void should_not_generate_client_operation_responses(string name) =>
        typeof(Methods).GetMethod(name).GetResponseModel().HasResponse.ShouldBeFalse();

    [Fact] void should_select_only_the_ordinary_tuple_response() =>
        typeof(Methods).GetMethod(nameof(Methods.Composed)).GetResponseModel().ResponseModel.Type.ShouldEqual(typeof(string));

    [Fact] void should_preserve_an_ordinary_nullable_response() =>
        typeof(Methods).GetMethod(nameof(Methods.Ordinary)).GetResponseModel().HasResponse.ShouldBeTrue();

    [Theory]
    [InlineData(nameof(Methods.Bare))]
    [InlineData(nameof(Methods.BareBatches))]
    public void should_reject_bare_nullable_operation_collections(string name) =>
        Specifications.Catch.Exception(() => typeof(Methods).GetMethod(name).GetResponseModel())
            .ShouldBeOfExactType<UnsupportedCommandOperationCollection>();

    public readonly record struct Operation : ICommandOperation
    {
        public void Execute() { }
    }

    public static class Methods
    {
        public static CommandOperations? NullableBatch() => null;
        public static Operation? NullableOperation() => null;
        public static Task<CommandOperations?> TaskBatch() => Task.FromResult<CommandOperations?>(default(CommandOperations));
        public static ValueTask<Operation?> ValueTaskOperation() => new(default(Operation));
        public static OneOf<CommandOperations?, ValidationResult> WrappedBatch() => default(CommandOperations);
        public static OneOf<Operation?, ValidationResult> WrappedOperation() => default(Operation);
        public static (Operation? Operation, CommandOperations? Operations, string Response) Composed() => (default(Operation), default(CommandOperations), "response");
        public static int? Ordinary() => null;
        public static Operation?[] Bare() => [];
        public static CommandOperations?[] BareBatches() => [];
    }
}
