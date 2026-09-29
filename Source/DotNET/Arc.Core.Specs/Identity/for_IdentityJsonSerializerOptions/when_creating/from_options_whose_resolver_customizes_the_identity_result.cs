// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Cratis.Arc.Identity.for_IdentityJsonSerializerOptions.when_creating;

/// <summary>
/// An application resolver that customizes the identity result contract keeps winning over Arc's own context, as it
/// did before Arc's identity types were source generated.
/// </summary>
public class from_options_whose_resolver_customizes_the_identity_result : Specification
{
    ArcOptions _arcOptions;
    JsonSerializerOptions _options;
    string _json;

    void Establish()
    {
        _arcOptions = new();
        _arcOptions.JsonSerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { RenameId }
        };
    }

    void Because()
    {
        _options = IdentityJsonSerializerOptions.CreateFrom(_arcOptions.JsonSerializerOptions, () => new DefaultJsonTypeInfoResolver());
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
