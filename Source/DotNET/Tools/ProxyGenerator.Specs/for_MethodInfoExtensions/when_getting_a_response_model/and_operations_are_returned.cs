// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Monads;

namespace Cratis.Arc.ProxyGenerator.for_MethodInfoExtensions.when_getting_a_response_model;

public class and_operations_are_returned : Specification
{
    [Theory]
    [InlineData(nameof(Methods.Concrete))]
    [InlineData(nameof(Methods.Interface))]
    [InlineData(nameof(Methods.Batch))]
    [InlineData(nameof(Methods.Wrapped))]
    [InlineData(nameof(Methods.ValueTask))]
    public void should_not_generate_client_operation_responses(string name) => typeof(Methods).GetMethod(name).GetResponseModel().HasResponse.ShouldBeFalse();

    [Fact] void should_keep_the_ordinary_response() => typeof(Methods).GetMethod(nameof(Methods.Composed)).GetResponseModel().HasResponse.ShouldBeTrue();
    [Fact] void should_reject_bare_operation_collections() => Specifications.Catch.Exception(() => typeof(Methods).GetMethod(nameof(Methods.Bare)).GetResponseModel()).ShouldBeOfExactType<UnsupportedCommandOperationCollection>();

    public record Operation : ICommandOperation
    {
        public void Execute() { }
    }

    public static class Methods
    {
        public static Operation? Concrete() => null;
        public static ICommandOperation? Interface() => null;
        public static CommandOperations Batch() => [];
        public static Result<CommandOperations, ValidationResult> Wrapped() => CommandOperations.Create([]);
        public static ValueTask<ICommandOperation> ValueTask() => new(new Operation());
        public static (Operation Operation, string[] Response) Composed() => (new(), []);
        public static Operation[] Bare() => [];
    }
}
