// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

public class when_performing_query_for_all_types : given.a_scenario_web_application
{
    QueryExecutionResult<AllTypesReadModel>? _executionResult;
    dynamic? _jsData;

    void Establish()
    {
        LoadQueryProxy<AllTypesReadModel>("GetWithAllTypes", "/tmp/AllTypesReadModel.GetWithAllTypes.ts");
    }

    async Task Because()
    {
        _executionResult = await Bridge.PerformQueryViaProxyAsync<AllTypesReadModel>("GetWithAllTypes");

        // Get the raw JavaScript data object to inspect types
        var dataJson = Bridge.Runtime.Evaluate<string>("JSON.stringify(__queryResult.data)");
        _jsData = System.Text.Json.JsonSerializer.Deserialize<System.Dynamic.ExpandoObject>(dataJson, Json.Globals.JsonSerializerOptions);
    }

    [Fact] void should_return_successful_result() => _executionResult.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_data() => _executionResult.Result.Data.ShouldNotBeNull();

    [Fact]
    void should_have_byte_as_number() =>
        Bridge.Runtime.Evaluate<string>("typeof __queryResult.data.byteValue").ShouldEqual("number");

    [Fact]
    void should_have_correct_byte_value() =>
        Bridge.Runtime.Evaluate<int>("__queryResult.data.byteValue").ShouldEqual(255);

    [Fact]
    void should_have_sbyte_as_number() =>
        Bridge.Runtime.Evaluate<string>("typeof __queryResult.data.signedByteValue").ShouldEqual("number");

    [Fact]
    void should_have_correct_sbyte_value() =>
        Bridge.Runtime.Evaluate<int>("__queryResult.data.signedByteValue").ShouldEqual(-128);

    [Fact]
    void should_have_short_as_number() =>
        Bridge.Runtime.Evaluate<string>("typeof __queryResult.data.shortValue").ShouldEqual("number");

    [Fact]
    void should_have_correct_short_value() =>
        Bridge.Runtime.Evaluate<int>("__queryResult.data.shortValue").ShouldEqual(-32768);

    [Fact]
    void should_have_int_as_number() =>
        Bridge.Runtime.Evaluate<string>("typeof __queryResult.data.intValue").ShouldEqual("number");

    [Fact]
    void should_have_correct_int_value() =>
        Bridge.Runtime.Evaluate<int>("__queryResult.data.intValue").ShouldEqual(42);

    [Fact]
    void should_have_long_as_number() =>
        Bridge.Runtime.Evaluate<string>("typeof __queryResult.data.longValue").ShouldEqual("number");

    [Fact]
    void should_have_correct_long_value() =>
        Bridge.Runtime.Evaluate<double>("__queryResult.data.longValue").ShouldEqual(9223372036854775807L);

    [Fact]
    void should_have_ushort_as_number() =>
        Bridge.Runtime.Evaluate<string>("typeof __queryResult.data.unsignedShortValue").ShouldEqual("number");

    [Fact]
    void should_have_correct_ushort_value() =>
        Bridge.Runtime.Evaluate<int>("__queryResult.data.unsignedShortValue").ShouldEqual(65535);

    [Fact]
    void should_have_uint_as_number() =>
        Bridge.Runtime.Evaluate<string>("typeof __queryResult.data.unsignedIntValue").ShouldEqual("number");
    [Fact]
    void should_have_correct_uint_value() =>
        Bridge.Runtime.Evaluate<long>("__queryResult.data.unsignedIntValue").ShouldEqual(4294967295);

    [Fact]
    void should_have_ulong_as_number() =>
        Bridge.Runtime.Evaluate<string>("typeof __queryResult.data.unsignedLongValue").ShouldEqual("number");

    [Fact]
    void should_have_float_as_number() =>
        Bridge.Runtime.Evaluate<string>("typeof __queryResult.data.floatValue").ShouldEqual("number");

    [Fact]
    void should_have_correct_float_value() =>
        Bridge.Runtime.Evaluate<double>("__queryResult.data.floatValue").ShouldBeInRange(3.13, 3.15);

    [Fact]
    void should_have_double_as_number() =>
        Bridge.Runtime.Evaluate<string>("typeof __queryResult.data.doubleValue").ShouldEqual("number");

    [Fact]
    void should_have_correct_double_value() =>
        Bridge.Runtime.Evaluate<double>("__queryResult.data.doubleValue").ShouldBeInRange(2.71, 2.72);

    [Fact]
    void should_have_decimal_as_number() =>
        Bridge.Runtime.Evaluate<string>("typeof __queryResult.data.decimalValue").ShouldEqual("number");

    [Fact]
    void should_have_correct_decimal_value() =>
        Bridge.Runtime.Evaluate<double>("__queryResult.data.decimalValue").ShouldBeInRange(123.45, 123.46);

    [Fact]
    void should_have_string_as_string() =>
        Bridge.Runtime.Evaluate<string>("typeof __queryResult.data.stringValue").ShouldEqual("string");

    [Fact]
    void should_have_correct_string_value() =>
        Bridge.Runtime.Evaluate<string>("__queryResult.data.stringValue").ShouldEqual("Test String");

    [Fact]
    void should_have_char_as_string() =>
        Bridge.Runtime.Evaluate<string>("typeof __queryResult.data.charValue").ShouldEqual("string");

    [Fact]
    void should_have_correct_char_value() =>
        Bridge.Runtime.Evaluate<string>("__queryResult.data.charValue").ShouldEqual("X");

    [Fact]
    void should_have_boolean_as_boolean() =>
        Bridge.Runtime.Evaluate<string>("typeof __queryResult.data.booleanValue").ShouldEqual("boolean");

    [Fact]
    void should_have_correct_boolean_value() =>
        Bridge.Runtime.Evaluate<bool>("__queryResult.data.booleanValue").ShouldBeTrue();

    [Fact]
    void should_have_datetime_value() =>
        Bridge.Runtime.Evaluate<bool>("__queryResult.data.dateTimeValue !== null && __queryResult.data.dateTimeValue !== undefined").ShouldBeTrue();

    [Fact]
    void should_have_datetimeoffset_value() =>
        Bridge.Runtime.Evaluate<bool>("__queryResult.data.dateTimeOffsetValue !== null && __queryResult.data.dateTimeOffsetValue !== undefined").ShouldBeTrue();

    [Fact]
    void should_have_dateonly_value() =>
        Bridge.Runtime.Evaluate<bool>("__queryResult.data.dateOnlyValue !== null && __queryResult.data.dateOnlyValue !== undefined").ShouldBeTrue();

    [Fact]
    void should_have_timeonly_value() =>
        Bridge.Runtime.Evaluate<bool>("__queryResult.data.timeOnlyValue !== null && __queryResult.data.timeOnlyValue !== undefined").ShouldBeTrue();

    [Fact]
    void should_have_guid_as_string() =>
        Bridge.Runtime.Evaluate<string>("typeof __queryResult.data.id").ShouldEqual("string");

    [Fact]
    void should_have_correct_guid_value() =>
        Bridge.Runtime.Evaluate<string>("__queryResult.data.id").ShouldEqual("12345678-1234-1234-1234-123456789abc");

    [Fact]
    void should_have_guid_value_as_string() =>
        Bridge.Runtime.Evaluate<string>("typeof __queryResult.data.guidValue").ShouldEqual("string");

    [Fact]
    void should_have_correct_guid_value_property() =>
        Bridge.Runtime.Evaluate<string>("__queryResult.data.guidValue").ShouldEqual("87654321-4321-4321-4321-cba987654321");

    [Fact]
    void should_have_timespan_as_string() =>
        Bridge.Runtime.Evaluate<string>("typeof __queryResult.data.timeSpanValue").ShouldEqual("string");

    [Fact]
    void should_have_uri_value() =>
        Bridge.Runtime.Evaluate<bool>("__queryResult.data.uriValue !== null && __queryResult.data.uriValue !== undefined").ShouldBeTrue();

    [Fact]
    void should_have_json_node_value() =>
        Bridge.Runtime.Evaluate<bool>("__queryResult.data.jsonNodeValue !== null && __queryResult.data.jsonNodeValue !== undefined").ShouldBeTrue();

    [Fact]
    void should_have_json_node_as_object() =>
        Bridge.Runtime.Evaluate<string>("typeof __queryResult.data.jsonNodeValue").ShouldEqual("object");

    [Fact]
    void should_have_json_node_nested_property() =>
        Bridge.Runtime.Evaluate<string>("__queryResult.data.jsonNodeValue.nested").ShouldEqual("value");

    [Fact]
    void should_have_json_node_number_property() =>
        Bridge.Runtime.Evaluate<int>("__queryResult.data.jsonNodeValue.number").ShouldEqual(42);

    [Fact]
    void should_have_json_object_value() =>
        Bridge.Runtime.Evaluate<bool>("__queryResult.data.jsonObjectValue !== null && __queryResult.data.jsonObjectValue !== undefined").ShouldBeTrue();

    [Fact]
    void should_have_json_object_as_object() =>
        Bridge.Runtime.Evaluate<string>("typeof __queryResult.data.jsonObjectValue").ShouldEqual("object");

    [Fact]
    void should_have_json_object_key1_property() =>
        Bridge.Runtime.Evaluate<string>("__queryResult.data.jsonObjectValue.key1").ShouldEqual("value1");

    [Fact]
    void should_have_json_object_key2_property() =>
        Bridge.Runtime.Evaluate<int>("__queryResult.data.jsonObjectValue.key2").ShouldEqual(123);

    [Fact]
    void should_have_json_array_value() =>
        Bridge.Runtime.Evaluate<bool>("__queryResult.data.jsonArrayValue !== null && __queryResult.data.jsonArrayValue !== undefined").ShouldBeTrue();

    [Fact]
    void should_have_json_array_as_array() =>
        Bridge.Runtime.Evaluate<bool>("Array.isArray(__queryResult.data.jsonArrayValue)").ShouldBeTrue();

    [Fact]
    void should_have_json_array_with_correct_length() =>
        Bridge.Runtime.Evaluate<int>("__queryResult.data.jsonArrayValue.length").ShouldEqual(4);

    [Fact]
    void should_have_json_array_first_element() =>
        Bridge.Runtime.Evaluate<int>("__queryResult.data.jsonArrayValue[0]").ShouldEqual(1);

    [Fact]
    void should_have_json_array_last_element() =>
        Bridge.Runtime.Evaluate<string>("__queryResult.data.jsonArrayValue[3]").ShouldEqual("four");

    [Fact]
    void should_have_json_document_value() =>
        Bridge.Runtime.Evaluate<bool>("__queryResult.data.jsonDocumentValue !== null && __queryResult.data.jsonDocumentValue !== undefined").ShouldBeTrue();

    [Fact]
    void should_have_json_document_as_object() =>
        Bridge.Runtime.Evaluate<string>("typeof __queryResult.data.jsonDocumentValue").ShouldEqual("object");

    [Fact]
    void should_have_json_document_document_property() =>
        Bridge.Runtime.Evaluate<string>("__queryResult.data.jsonDocumentValue.document").ShouldEqual("data");

    [Fact]
    void should_have_json_document_items_as_array() =>
        Bridge.Runtime.Evaluate<bool>("Array.isArray(__queryResult.data.jsonDocumentValue.items)").ShouldBeTrue();

    [Fact]
    void should_have_object_value() =>
        Bridge.Runtime.Evaluate<bool>("__queryResult.data.objectValue !== null && __queryResult.data.objectValue !== undefined").ShouldBeTrue();

    [Fact]
    void should_have_object_as_object() =>
        Bridge.Runtime.Evaluate<string>("typeof __queryResult.data.objectValue").ShouldEqual("object");

    [Fact]
    void should_have_object_dynamic_property() =>
        Bridge.Runtime.Evaluate<string>("__queryResult.data.objectValue.dynamic").ShouldEqual("Content");

    [Fact]
    void should_have_object_value_property() =>
        Bridge.Runtime.Evaluate<int>("__queryResult.data.objectValue.value").ShouldEqual(999);
}
