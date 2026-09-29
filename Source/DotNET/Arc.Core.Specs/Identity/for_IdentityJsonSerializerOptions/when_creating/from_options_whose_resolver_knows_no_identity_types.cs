// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Cratis.Arc.Identity.for_IdentityJsonSerializerOptions.when_creating;

/// <summary>
/// An application that configures its own resolver - as a trimmed or NativeAOT application does - still gets Arc's
/// identity types from Arc's source-generated context, while its identity details must come from its own resolver.
/// </summary>
public class from_options_whose_resolver_knows_no_identity_types : Specification
{
    ArcOptions _arcOptions;
    JsonSerializerOptions _options;
    string _json;
    IdentityProviderResult? _roundTripped;
    Exception? _unknownDetailsError;

    void Establish()
    {
        _arcOptions = new();
        _arcOptions.JsonSerializerOptions.TypeInfoResolver = JsonTypeInfoResolver.Combine();
    }

    void Because()
    {
        _options = IdentityJsonSerializerOptions.CreateFrom(_arcOptions.JsonSerializerOptions);
        var result = new IdentityProviderResult("user-1", "User", true, true, ["Admin"], default!);
        _json = JsonSerializer.Serialize(result, (JsonTypeInfo<IdentityProviderResult>)_options.GetTypeInfo(typeof(IdentityProviderResult)));
        _roundTripped = JsonSerializer.Deserialize(_json, (JsonTypeInfo<IdentityProviderResult>)_options.GetTypeInfo(typeof(IdentityProviderResult)));
        _unknownDetailsError = Catch.Exception(() => _options.GetTypeInfo(typeof(UnknownDetails)));
    }

    [Fact] void should_serialize_the_identity_result_with_the_arc_conventions() => _json.ShouldEqual("{\"id\":\"user-1\",\"name\":\"User\",\"isAuthenticated\":true,\"isAuthorized\":true,\"roles\":[\"Admin\"]}");
    [Fact] void should_deserialize_the_identity_result() => _roundTripped!.Id.ShouldEqual(new IdentityId("user-1"));
    [Fact] void should_leave_details_to_the_application_resolver() => _unknownDetailsError.ShouldBeOfExactType<NotSupportedException>();
    [Fact] void should_be_read_only() => _options.IsReadOnly.ShouldBeTrue();
    [Fact] void should_not_fall_back_to_reflection() => _options.TypeInfoResolverChain.ShouldNotContain(JsonSerializerOptions.Default.TypeInfoResolver);

    public record UnknownDetails(string Value);
}
