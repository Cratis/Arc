// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_performing;

public class with_case_insensitive_parameter_matching : given.a_model_bound_query_performer
{
    public record TestReadModel
    {
        public static string ReceivedName { get; set; } = string.Empty;
        public static int ReceivedAge { get; set; }

        public static TestReadModel Query(string name, int age)
        {
            ReceivedName = name;
            ReceivedAge = age;
            return new TestReadModel();
        }
    }

    void Establish()
    {
        var parameters = new QueryArguments
        {
            ["NAME"] = "Jane Smith",
            ["Age"] = "30"
        };

        EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query), parameters: parameters);
    }

    async Task Because() => await PerformQuery();

    [Fact] void should_match_name_parameter_case_insensitive() => TestReadModel.ReceivedName.ShouldEqual("Jane Smith");
    [Fact] void should_match_age_parameter_case_insensitive() => TestReadModel.ReceivedAge.ShouldEqual(30);
}