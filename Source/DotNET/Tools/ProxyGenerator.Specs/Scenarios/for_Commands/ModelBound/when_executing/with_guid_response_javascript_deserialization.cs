// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound.when_executing;

/// <summary>
/// Verifies that the generated TypeScript type with Guid properties has @field metadata
/// properly registered and that JsonSerializer.deserializeFromInstance converts Guid strings
/// to Guid objects.
/// </summary>
[Collection(ScenarioCollectionDefinition.Name)]
public class with_guid_response_javascript_deserialization : given.a_scenario_web_application
{
    int _fieldsCount;
    string _userIdType = string.Empty;
    string _userIdValue = string.Empty;
    string _nameValue = string.Empty;
    string _sessionIdType = string.Empty;
    string _sessionIdValue = string.Empty;

    void Establish() => LoadCommandProxy<CommandWithGuidResponse>();

    void Because()
    {
        // Create a raw object with Guid strings (simulating server response)
        // and deserialize it using JsonSerializer.deserializeFromInstance
        Runtime!.Execute(
            "var __rawData = { userId: '12345678-1234-1234-1234-123456789abc', name: 'TestUser', sessionId: '87654321-4321-4321-4321-cba987654321' };" +
            "var __deserialized = globalThis.JsonSerializer.deserializeFromInstance(globalThis.GuidResponseData, __rawData);" +
            "var __fieldsForType = globalThis.Fields.getFieldsForType(globalThis.GuidResponseData);");

        _fieldsCount = Runtime.Evaluate<int>("__fieldsForType.length");
        _userIdType = Runtime.Evaluate<string>("typeof __deserialized.userId") ?? string.Empty;
        _userIdValue = Runtime.Evaluate<string>("String(__deserialized.userId)") ?? string.Empty;
        _nameValue = Runtime.Evaluate<string>("__deserialized.name") ?? string.Empty;
        _sessionIdType = Runtime.Evaluate<string>("typeof __deserialized.sessionId") ?? string.Empty;
        _sessionIdValue = Runtime.Evaluate<string>("String(__deserialized.sessionId)") ?? string.Empty;
    }

    [Fact] void should_have_three_registered_fields() => _fieldsCount.ShouldEqual(3);
    [Fact] void should_have_user_id_as_object() => _userIdType.ShouldEqual("object");
    [Fact] void should_have_correct_user_id_value() => _userIdValue.ShouldEqual("12345678-1234-1234-1234-123456789abc");
    [Fact] void should_have_correct_name() => _nameValue.ShouldEqual("TestUser");
    [Fact] void should_have_session_id_as_object() => _sessionIdType.ShouldEqual("object");
    [Fact] void should_have_correct_session_id_value() => _sessionIdValue.ShouldEqual("87654321-4321-4321-4321-cba987654321");
}
