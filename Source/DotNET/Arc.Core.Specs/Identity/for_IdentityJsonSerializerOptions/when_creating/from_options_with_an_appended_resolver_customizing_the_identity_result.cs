// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Cratis.Arc.Identity.for_IdentityJsonSerializerOptions.when_creating;

/// <summary>
/// A resolver an application appends to Arc's resolver chain to customize the identity result contract keeps winning
/// for the identity options, which put Arc's identity context behind Arc's resolver.
/// </summary>
public class from_options_with_an_appended_resolver_customizing_the_identity_result : Specification
{
    ArcOptions _arcOptions;
    JsonSerializerOptions _options;
    string _json;

    void Establish()
    {
        _arcOptions = new();
        _arcOptions.JsonSerializerOptions.TypeInfoResolverChain.Add(new DefaultJsonTypeInfoResolver
        {
            Modifiers = { RenameId }
        });
    }

    void Because()
    {
        _options = IdentityJsonSerializerOptions.CreateFrom(_arcOptions.JsonSerializerOptions);
        var result = new IdentityProviderResult("user-1", "User", true, true, ["Admin"], default!);
        _json = JsonSerializer.Serialize(result, (JsonTypeInfo<IdentityProviderResult>)_options.GetTypeInfo(typeof(IdentityProviderResult)));
    }

    [Fact] void should_reflect_the_application_customization() => _json.ShouldEqual("{\"identifier\":\"user-1\",\"name\":\"User\",\"isAuthenticated\":true,\"isAuthorized\":true,\"roles\":[\"Admin\"]}");

    static void RenameId(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type != typeof(IdentityProviderResult))
        {
            return;
        }

        foreach (var property in typeInfo.Properties.Where(_ => _.Name == "id"))
        {
            property.Name = "identifier";
        }
    }
}
