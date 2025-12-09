// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_performing;

public class with_query_string_parameters_matching_method_parameters : given.a_model_bound_query_performer
{
    public record TestReadModel
    {
        public static string ReceivedName { get; set; } = string.Empty;
        public static int ReceivedAge { get; set; }
        public static bool ReceivedIsActive { get; set; }

        public static TestReadModel Query(string name, int age, bool isActive)
        {
            ReceivedName = name;
            ReceivedAge = age;
            ReceivedIsActive = isActive;
            return new TestReadModel();
        }
    }

    void Establish()
    {
        var parameters = new QueryArguments
        {
            ["name"] = "John Doe",
            ["age"] = "25",
            ["isActive"] = "true"
        };

        EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query), parameters: parameters);
    }

    async Task Because() => await PerformQuery();

    [Fact] void should_match_name_parameter() => TestReadModel.ReceivedName.ShouldEqual("John Doe");
    [Fact] void should_match_age_parameter() => TestReadModel.ReceivedAge.ShouldEqual(25);
    [Fact] void should_match_is_active_parameter() => TestReadModel.ReceivedIsActive.ShouldBeTrue();
    [Fact] void should_return_result() => _result.ShouldNotBeNull();
}