// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Cratis.Arc.Identity.for_IdentityJsonSerializerOptions.when_creating;

/// <summary>
/// A NativeAOT application configures a source-generated context that knows its identity details but not Arc's
/// identity result. The result resolves through Arc's context and the details through the application's, without
/// reflection.
/// </summary>
public partial class from_options_whose_resolver_is_a_generated_context_knowing_the_details : Specification
{
    ArcOptions _arcOptions;
    JsonSerializerOptions _options;
    string _json;

    void Establish()
    {
        _arcOptions = new();
        _arcOptions.JsonSerializerOptions.TypeInfoResolver = ApplicationDetailsContext.Default;
    }

    void Because()
    {
        _options = IdentityJsonSerializerOptions.CreateFrom(_arcOptions.JsonSerializerOptions);
        var result = new IdentityProviderResult("user-1", "User", true, true, ["Admin"], new KnownDetails("R&D"));
        _json = JsonSerializer.Serialize(result, (JsonTypeInfo<IdentityProviderResult>)_options.GetTypeInfo(typeof(IdentityProviderResult)));
    }

    [Fact] void should_serialize_the_identity_result_and_the_details() => _json.ShouldEqual("{\"id\":\"user-1\",\"name\":\"User\",\"isAuthenticated\":true,\"isAuthorized\":true,\"roles\":[\"Admin\"],\"details\":{\"department\":\"R&D\"}}");
    [Fact] void should_not_fall_back_to_reflection() => _options.TypeInfoResolverChain.ShouldNotContain(JsonSerializerOptions.Default.TypeInfoResolver);

    public record KnownDetails(string Department);

    [JsonSerializable(typeof(KnownDetails))]
    public partial class ApplicationDetailsContext : JsonSerializerContext;
}
