// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_performing;

public class with_type_conversion_from_query_strings : given.a_model_bound_query_performer
{
    public record TestReadModel
    {
        public static int ReceivedIntValue { get; set; }
        public static bool ReceivedBoolValue { get; set; }
        public static decimal ReceivedDecimalValue { get; set; }
        public static DateTime ReceivedDateTimeValue { get; set; }
        public static Guid ReceivedGuidValue { get; set; }

        public static TestReadModel Query(int intValue, bool boolValue, decimal decimalValue, DateTime dateTimeValue, Guid guidValue)
        {
            ReceivedIntValue = intValue;
            ReceivedBoolValue = boolValue;
            ReceivedDecimalValue = decimalValue;
            ReceivedDateTimeValue = dateTimeValue;
            ReceivedGuidValue = guidValue;
            return new TestReadModel();
        }
    }

    Guid _expectedGuid = Guid.NewGuid();
    DateTime _expectedDateTime = new(2023, 12, 25, 10, 30, 0);

    void Establish()
    {
        var parameters = new QueryArguments
        {
            ["intValue"] = "42",
            ["boolValue"] = "true",
            ["decimalValue"] = "123.45",
            ["dateTimeValue"] = _expectedDateTime.ToString("O"),
            ["guidValue"] = _expectedGuid.ToString()
        };

        EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query), parameters: parameters);
    }

    async Task Because() => await PerformQuery();

    [Fact] void should_convert_int_value() => TestReadModel.ReceivedIntValue.ShouldEqual(42);
    [Fact] void should_convert_bool_value() => TestReadModel.ReceivedBoolValue.ShouldBeTrue();
    [Fact] void should_convert_decimal_value() => TestReadModel.ReceivedDecimalValue.ShouldEqual(123.45m);
    [Fact] void should_convert_datetime_value() => TestReadModel.ReceivedDateTimeValue.ShouldEqual(_expectedDateTime);
    [Fact] void should_convert_guid_value() => TestReadModel.ReceivedGuidValue.ShouldEqual(_expectedGuid);
}