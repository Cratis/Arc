// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_performing;

public class with_nullable_parameters : given.a_model_bound_query_performer
{
    public record TestReadModel
    {
        public static int? ReceivedNullableInt { get; set; }
        public static string? ReceivedNullableString { get; set; }
        public static bool? ReceivedNullableBool { get; set; }

        public static TestReadModel Query(int? nullableInt, string? nullableString, bool? nullableBool)
        {
            ReceivedNullableInt = nullableInt;
            ReceivedNullableString = nullableString;
            ReceivedNullableBool = nullableBool;
            return new TestReadModel();
        }
    }

    void Establish()
    {
        var parameters = new QueryArguments
        {
            ["nullableInt"] = "100",
            ["nullableString"] = "nullable string",
            ["nullableBool"] = "false"
        };

        EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query), parameters: parameters);
    }

    async Task Because() => await PerformQuery();

    [Fact] void should_convert_nullable_int() => TestReadModel.ReceivedNullableInt.ShouldEqual(100);
    [Fact] void should_convert_nullable_string() => TestReadModel.ReceivedNullableString.ShouldEqual("nullable string");
    [Fact] void should_convert_nullable_bool() => TestReadModel.ReceivedNullableBool.ShouldEqual(false);
}