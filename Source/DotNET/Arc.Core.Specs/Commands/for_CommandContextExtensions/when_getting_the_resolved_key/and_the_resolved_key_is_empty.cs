// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandContextExtensions.when_getting_the_resolved_key;

/// <summary>
/// An empty key is the verdict of an integration that owns key resolution — Chronicle writes one when the command
/// carried nothing usable — and is read back as it was written rather than as an absent key.
/// </summary>
public class and_the_resolved_key_is_empty : Specification
{
    string? _result;

    void Because() => _result = ContextWith(new CommandContextValues { { CommandContextKeys.ResolvedKey, string.Empty } }).GetResolvedKey();

    [Fact] void should_read_back_the_empty_key() => _result.ShouldEqual(string.Empty);

    static CommandContext ContextWith(CommandContextValues values) =>
        new(CorrelationId.New(), typeof(object), new object(), [], values);
}
