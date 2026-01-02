// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]

public class when_performing_query_for_all_types : given.a_scenario_web_application
{
    QueryExecutionResult<AllTypesReadModel>? _executionResult;
    IDictionary<string, object?>? _data;
    IDictionary<string, object?>? _types;

    void Establish()
    {
        LoadQueryProxy<AllTypesReadModel>("GetWithAllTypes", "/tmp/GetWithAllTypes.ts");
    }

    async Task Because()
    {
        await File.WriteAllTextAsync("/tmp/because-start.txt", "Because started");
        _executionResult = await Bridge.PerformQueryViaProxyAsync<AllTypesReadModel>("GetWithAllTypes");
        await File.WriteAllTextAsync("/tmp/because-after-query.txt", "After query");

        try
        {
            // Debug: Check the actual modelType used by Query and whether it has fields
            var modelTypeDebug = Bridge.Runtime.Evaluate<string>("\n" +
                                                                 "                JSON.stringify({\n" +
                                                                 "                    queryModelTypeName: __query.modelType ? __query.modelType.name : 'none',\n" +
                                                                 "                    queryModelTypeHasFields: __query.modelType && globalThis.Fields ? globalThis.Fields.getFieldsForType(__query.modelType).length : -1,\n" +
                                                                 "                    globalModelTypeName: globalThis.AllTypesReadModel ? globalThis.AllTypesReadModel.name : 'none',\n" +
                                                                 "                    globalModelTypeHasFields: globalThis.AllTypesReadModel && globalThis.Fields ? globalThis.Fields.getFieldsForType(globalThis.AllTypesReadModel).length : -1,\n" +
                                                                 "                    areTheSame: __query.modelType === globalThis.AllTypesReadModel\n" +
                                                                 "                })\n" +
                                                                 "            ");
            await File.WriteAllTextAsync("/tmp/model-type-comparison.json", modelTypeDebug);
        }
        catch (Exception ex)
        {
            await File.WriteAllTextAsync("/tmp/model-type-error.txt", ex.ToString());
        }

        await File.WriteAllTextAsync("/tmp/because-end.txt", "Because finished");

        // Debug: Trace the deserialization in QueryResult
#pragma warning disable MA0101 // String contains an implicit end of line character
        var deserializationTrace = Bridge.Runtime.Evaluate<string>(@"
            JSON.stringify({
                resultExists: typeof __queryResult !== 'undefined',
                resultData: typeof __queryResult !== 'undefined' ? __queryResult.data : null,
                resultDataKeys: typeof __queryResult !== 'undefined' && __queryResult.data ? Object.keys(__queryResult.data) : [],
                queryExists: typeof __query !== 'undefined',
                queryModelType: typeof __query !== 'undefined' && __query.modelType ? __query.modelType.name : 'N/A',
                manualDeserializationTest: (() => {
                    if (typeof __query === 'undefined' || !__query.modelType || typeof globalThis.JsonSerializer === 'undefined') return null;
                    try {
                        // Get the raw response data from the fetch
                        const rawData = JSON.parse(JSON.stringify(__queryResult));
                        if (rawData.data && typeof rawData.data === 'object') {
                            const deserialized = globalThis.JsonSerializer.deserializeFromInstance(__query.modelType, rawData.data);
                            return {
                                success: true,
                                keys: Object.keys(deserialized),
                                hasId: 'id' in deserialized,
                                idValue: deserialized.id
                            };
                        }
                        return {success: false, reason: 'no data'};
                    } catch(e) {
                        return {success: false, error: e.message, stack: e.stack};
                    }
                })()
            })
        ");
#pragma warning restore MA0101 // String contains an implicit end of line character
        await File.WriteAllTextAsync("/tmp/deserialization-trace.json", deserializationTrace);

        // Debug: Check what the full queryResult looks like
        var rawQueryResult = Bridge.Runtime.Evaluate<string>("JSON.stringify(__queryResult)");
        await File.WriteAllTextAsync("/tmp/raw-query-result.json", rawQueryResult);

        // Get the data
        var dataJson = Bridge.Runtime.Evaluate<string>("JSON.stringify(__queryResult.data)");
        await File.WriteAllTextAsync("/tmp/all-types-data.json", dataJson);
        _data = System.Text.Json.JsonSerializer.Deserialize<System.Dynamic.ExpandoObject>(dataJson, Json.Globals.JsonSerializerOptions);

        // Debug: Check Fields metadata
#pragma warning disable MA0101 // String contains an implicit end of line character
        var fieldsDebug = Bridge.Runtime.Evaluate<string>(@"
            JSON.stringify({
                isFieldsDefined: typeof globalThis.Fields !== 'undefined',
                isGetFieldsForType: typeof globalThis.Fields !== 'undefined' && typeof globalThis.Fields.getFieldsForType === 'function',
                allTypesReadModelExists: typeof globalThis.AllTypesReadModel !== 'undefined',
                fieldsCount: typeof globalThis.Fields !== 'undefined' && typeof globalThis.AllTypesReadModel !== 'undefined' && globalThis.Fields.getFieldsForType ? globalThis.Fields.getFieldsForType(globalThis.AllTypesReadModel).length : -1,
                firstField: typeof globalThis.Fields !== 'undefined' && typeof globalThis.AllTypesReadModel !== 'undefined' && globalThis.Fields.getFieldsForType && globalThis.Fields.getFieldsForType(globalThis.AllTypesReadModel).length > 0 ? globalThis.Fields.getFieldsForType(globalThis.AllTypesReadModel)[0].name : 'none'
            })
        ");
#pragma warning restore MA0101 // String contains an implicit end of line character
        await File.WriteAllTextAsync("/tmp/fields-debug.json", fieldsDebug);

        // Get all typeof checks in a single evaluation - but check if jsonDocumentValue exists first
#pragma warning disable MA0101 // String contains an implicit end of line character
        var typesJson = Bridge.Runtime.Evaluate<string>(@"
            JSON.stringify({
                byteValue: typeof __queryResult.data.byteValue,
                signedByteValue: typeof __queryResult.data.signedByteValue,
                shortValue: typeof __queryResult.data.shortValue,
                intValue: typeof __queryResult.data.intValue,
                longValue: typeof __queryResult.data.longValue,
                unsignedShortValue: typeof __queryResult.data.unsignedShortValue,
                unsignedIntValue: typeof __queryResult.data.unsignedIntValue,
                unsignedLongValue: typeof __queryResult.data.unsignedLongValue,
                floatValue: typeof __queryResult.data.floatValue,
                doubleValue: typeof __queryResult.data.doubleValue,
                decimalValue: typeof __queryResult.data.decimalValue,
                stringValue: typeof __queryResult.data.stringValue,
                charValue: typeof __queryResult.data.charValue,
                booleanValue: typeof __queryResult.data.booleanValue,
                id: typeof __queryResult.data.id,
                guidValue: typeof __queryResult.data.guidValue,
                timeSpanValue: typeof __queryResult.data.timeSpanValue,
                jsonNodeValue: typeof __queryResult.data.jsonNodeValue,
                jsonObjectValue: typeof __queryResult.data.jsonObjectValue,
                jsonDocumentValue: typeof __queryResult.data.jsonDocumentValue,
                objectValue: typeof __queryResult.data.objectValue,
                isJsonArrayArray: Array.isArray(__queryResult.data.jsonArrayValue),
                isJsonDocumentItemsArray: __queryResult.data.jsonDocumentValue ? Array.isArray(__queryResult.data.jsonDocumentValue.items) : false
            })
        ");
#pragma warning restore MA0101 // String contains an implicit end of line character
        _types = System.Text.Json.JsonSerializer.Deserialize<System.Dynamic.ExpandoObject>(typesJson, Json.Globals.JsonSerializerOptions);
    }

    [Fact] void should_return_successful_result() => _executionResult.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_data() => _executionResult.Result.Data.ShouldNotBeNull();

    [Fact] void should_have_byte_as_number() => _types["byteValue"].ToString().ShouldEqual("number");
    [Fact] void should_have_correct_byte_value() => Convert.ToByte(((System.Text.Json.JsonElement)_data["byteValue"]!).GetInt32()).ShouldEqual((byte)255);

    [Fact] void should_have_sbyte_as_number() => _types["signedByteValue"].ToString().ShouldEqual("number");
    [Fact] void should_have_correct_sbyte_value() => Convert.ToSByte(((System.Text.Json.JsonElement)_data["signedByteValue"]!).GetInt32()).ShouldEqual((sbyte)-128);

    [Fact] void should_have_short_as_number() => _types["shortValue"].ToString().ShouldEqual("number");
    [Fact] void should_have_correct_short_value() => ((System.Text.Json.JsonElement)_data["shortValue"]!).GetInt32().ShouldEqual(-32768);

    [Fact] void should_have_int_as_number() => _types["intValue"].ToString().ShouldEqual("number");
    [Fact] void should_have_correct_int_value() => ((System.Text.Json.JsonElement)_data["intValue"]!).GetInt32().ShouldEqual(42);

    [Fact] void should_have_long_as_number() => _types["longValue"].ToString().ShouldEqual("number");
    [Fact] void should_have_correct_long_value() => ((System.Text.Json.JsonElement)_data["longValue"]!).GetDouble().ShouldEqual(9223372036854775807L);

    [Fact] void should_have_ushort_as_number() => _types["unsignedShortValue"].ToString().ShouldEqual("number");
    [Fact] void should_have_correct_ushort_value() => ((System.Text.Json.JsonElement)_data["unsignedShortValue"]!).GetInt32().ShouldEqual(65535);

    [Fact] void should_have_uint_as_number() => _types["unsignedIntValue"].ToString().ShouldEqual("number");
    [Fact] void should_have_correct_uint_value() => ((System.Text.Json.JsonElement)_data["unsignedIntValue"]!).GetInt64().ShouldEqual(4294967295);

    [Fact] void should_have_ulong_as_number() => _types["unsignedLongValue"].ToString().ShouldEqual("number");

    [Fact] void should_have_float_as_number() => _types["floatValue"].ToString().ShouldEqual("number");
    [Fact] void should_have_correct_float_value() => ((System.Text.Json.JsonElement)_data["floatValue"]!).GetDouble().ShouldBeInRange(3.13, 3.15);

    [Fact] void should_have_double_as_number() => _types["doubleValue"].ToString().ShouldEqual("number");
    [Fact] void should_have_correct_double_value() => ((System.Text.Json.JsonElement)_data["doubleValue"]!).GetDouble().ShouldBeInRange(2.71, 2.72);

    [Fact] void should_have_decimal_as_number() => _types["decimalValue"].ToString().ShouldEqual("number");
    [Fact] void should_have_correct_decimal_value() => ((System.Text.Json.JsonElement)_data["decimalValue"]!).GetDouble().ShouldBeInRange(123.45, 123.46);

    [Fact] void should_have_string_as_string() => _types["stringValue"].ToString().ShouldEqual("string");
    [Fact] void should_have_correct_string_value() => ((System.Text.Json.JsonElement)_data["stringValue"]!).GetString().ShouldEqual("Test String");

    [Fact] void should_have_char_as_string() => _types["charValue"].ToString().ShouldEqual("string");
    [Fact] void should_have_correct_char_value() => ((System.Text.Json.JsonElement)_data["charValue"]!).GetString().ShouldEqual("X");

    [Fact] void should_have_boolean_as_boolean() => _types["booleanValue"].ToString().ShouldEqual("boolean");
    [Fact] void should_have_correct_boolean_value() => ((System.Text.Json.JsonElement)_data["booleanValue"]!).GetBoolean().ShouldBeTrue();

    [Fact] void should_have_datetime_value() => _data["dateTimeValue"].ShouldNotBeNull();
    [Fact] void should_have_datetimeoffset_value() => _data["dateTimeOffsetValue"].ShouldNotBeNull();
    [Fact] void should_have_dateonly_value() => _data["dateOnlyValue"].ShouldNotBeNull();
    [Fact] void should_have_timeonly_value() => _data["timeOnlyValue"].ShouldNotBeNull();

    [Fact] void should_have_guid_as_string() => _types["id"].ToString().ShouldEqual("string");
    [Fact] void should_have_correct_guid_value() => ((System.Text.Json.JsonElement)_data["id"]!).GetString().ShouldEqual("12345678-1234-1234-1234-123456789abc");

    [Fact] void should_have_guid_value_as_string() => _types["guidValue"].ToString().ShouldEqual("string");
    [Fact] void should_have_correct_guid_value_property() => ((System.Text.Json.JsonElement)_data["guidValue"]!).GetString().ShouldEqual("87654321-4321-4321-4321-cba987654321");

    [Fact] void should_have_timespan_as_string() => _types["timeSpanValue"].ToString().ShouldEqual("string");
    [Fact] void should_have_uri_value() => _data["uriValue"].ShouldNotBeNull();

    [Fact] void should_have_json_node_value() => _data["jsonNodeValue"].ShouldNotBeNull();
    [Fact] void should_have_json_node_as_object() => _types["jsonNodeValue"].ToString().ShouldEqual("object");
    [Fact] void should_have_json_node_nested_property() => ((System.Text.Json.JsonElement)_data["jsonNodeValue"]!).GetProperty("nested").GetString().ShouldEqual("value");
    [Fact] void should_have_json_node_number_property() => ((System.Text.Json.JsonElement)_data["jsonNodeValue"]!).GetProperty("number").GetInt32().ShouldEqual(42);

    [Fact] void should_have_json_object_value() => _data["jsonObjectValue"].ShouldNotBeNull();
    [Fact] void should_have_json_object_as_object() => _types["jsonObjectValue"].ToString().ShouldEqual("object");
    [Fact] void should_have_json_object_key1_property() => ((System.Text.Json.JsonElement)_data["jsonObjectValue"]!).GetProperty("key1").GetString().ShouldEqual("value1");
    [Fact] void should_have_json_object_key2_property() => ((System.Text.Json.JsonElement)_data["jsonObjectValue"]!).GetProperty("key2").GetInt32().ShouldEqual(123);

    [Fact] void should_have_json_array_value() => _data["jsonArrayValue"].ShouldNotBeNull();
    [Fact] void should_have_json_array_as_array() => ((System.Text.Json.JsonElement)_types["isJsonArrayArray"]!).GetBoolean().ShouldBeTrue();
    [Fact] void should_have_json_array_with_correct_length() => ((System.Text.Json.JsonElement)_data["jsonArrayValue"]!).GetArrayLength().ShouldEqual(4);
    [Fact] void should_have_json_array_first_element() => ((System.Text.Json.JsonElement)_data["jsonArrayValue"]!)[0].GetInt32().ShouldEqual(1);
    [Fact] void should_have_json_array_last_element() => ((System.Text.Json.JsonElement)_data["jsonArrayValue"]!)[3].GetString().ShouldEqual("four");
    [Fact] void should_have_json_array_second_element() => ((System.Text.Json.JsonElement)_data["jsonArrayValue"]!)[1].GetInt32().ShouldEqual(2);
    [Fact] void should_have_json_array_third_element() => ((System.Text.Json.JsonElement)_data["jsonArrayValue"]!)[2].GetInt32().ShouldEqual(3);

    [Fact] void should_have_json_document_value() => _data["jsonDocumentValue"].ShouldNotBeNull();
    [Fact] void should_have_json_document_as_object() => _types["jsonDocumentValue"].ToString().ShouldEqual("object");
    [Fact] void should_have_json_document_document_property() => ((System.Text.Json.JsonElement)_data["jsonDocumentValue"]!).GetProperty("document").GetString().ShouldEqual("data");
    [Fact] void should_have_json_document_items_as_array() => ((System.Text.Json.JsonElement)_types["isJsonDocumentItemsArray"]!).GetBoolean().ShouldBeTrue();

    [Fact] void should_have_object_value() => _data["objectValue"].ShouldNotBeNull();
    [Fact] void should_have_object_as_object() => _types["objectValue"].ToString().ShouldEqual("object");
    [Fact] void should_have_object_dynamic_property() => ((System.Text.Json.JsonElement)_data["objectValue"]!).GetProperty("dynamic").GetString().ShouldEqual("Content");
    [Fact] void should_have_object_value_property() => ((System.Text.Json.JsonElement)_data["objectValue"]!).GetProperty("value").GetInt32().ShouldEqual(999);
}
