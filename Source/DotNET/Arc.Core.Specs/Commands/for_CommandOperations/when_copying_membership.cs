// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandOperations;

public class when_copying_membership : Specification
{
    ICommandOperation[] _source;
    CommandOperations _batch;
    ICommandOperation _original;

    void Establish()
    {
        _original = Substitute.For<ICommandOperation>();
        _source = [_original];
    }

    void Because()
    {
        _batch = new(_source);
        _source[0] = Substitute.For<ICommandOperation>();
    }

    [Fact] void should_copy_the_original_membership() => _batch[0].ShouldEqual(_original);
    [Fact] void should_treat_default_as_empty() => default(CommandOperations).ShouldBeEmpty();
    [Fact] void should_support_empty_collection_expressions() => CommandOperations.Create([]).ShouldEqual(default);
    [Fact] void should_reject_null_declarations() => Catch.Exception(() => _ = new CommandOperations([null!])).ShouldBeOfExactType<InvalidCommandOperation>();
    [Fact] void should_preserve_equality_by_ordered_membership() => _batch.ShouldEqual(CommandOperations.Create([_original]));
    [Fact] void should_not_expose_mutable_list_storage() => ((object)_batch is IList<ICommandOperation>).ShouldBeFalse();
}
