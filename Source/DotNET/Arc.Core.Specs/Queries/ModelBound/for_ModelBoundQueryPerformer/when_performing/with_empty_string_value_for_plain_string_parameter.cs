// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_performing;

/// <summary>
/// A plain string parameter should receive "" when the query string contains the key with an empty value.
/// Empty string is a meaningful value for string (unlike for concepts backed by non-string types).
/// </summary>
public class with_empty_string_value_for_plain_string_parameter : given.a_model_bound_query_performer
{
    public record TestReadModel
    {
        public static string ReceivedName { get; set; } = "not-set";

        public static TestReadModel Query(string name)
        {
            ReceivedName = name;
            return new();
        }
    }

    void Establish() => EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query), parameters: new QueryArguments { ["name"] = string.Empty });

    async Task Because() => await PerformQuery();

    [Fact] void should_not_throw() => _result.ShouldNotBeNull();
    [Fact] void should_pass_empty_string_to_the_method() => TestReadModel.ReceivedName.ShouldEqual(string.Empty);
}
