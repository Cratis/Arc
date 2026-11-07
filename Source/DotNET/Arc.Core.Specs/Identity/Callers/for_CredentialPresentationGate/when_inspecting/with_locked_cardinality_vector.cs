// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Identity.Callers.for_CredentialPresentationGate.when_inspecting;

/// <summary>Locked principals-and-workload row 28, read from the embedded vector, is refused by the gate.</summary>
public class with_locked_cardinality_vector : given.a_gate
{
    JsonElement _vector;

    void Establish()
    {
        using var stream = typeof(with_locked_cardinality_vector).Assembly.GetManifestResourceStream("CallerConstructionVectors.json")!;
        using var document = JsonDocument.Parse(stream);
        _vector = document.RootElement.GetProperty("vectors").EnumerateArray().Single(v => v.GetProperty("row").GetInt32() == 28).Clone();
        var given = _vector.GetProperty("given");
        var headers = new List<string>();
        if (given.GetProperty("sessionCookiePresent").GetBoolean())
        {
            headers.Add("Cookie");
            _raw.GetHeaderValues("Cookie").Returns(["cratis-identity-session=fixture"]);
        }
        if (given.GetProperty("authorizationBearerPresent").GetBoolean())
        {
            headers.Add("Authorization");
            _raw.GetHeaderValues("Authorization").Returns(["Bearer fixture"]);
        }
        _raw.HeaderNames.Returns(headers);
    }

    void Because() => _result = _gate.Inspect(_request);

    [Fact] void should_count_two_credentials() => _vector.GetProperty("then").GetProperty("credentialCount").GetInt32().ShouldEqual(2);
    [Fact] void should_refuse_as_the_vector_requires() => (_result.Refusal == CallerRefusal.MultipleCredentials).ShouldEqual(!_vector.GetProperty("then").GetProperty("accepted").GetBoolean());
    [Fact] void should_not_select_a_shape_so_no_caller_is_constructed() => _result.Shape.ShouldBeNull();
}
