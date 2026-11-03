// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// Parity of the two binders when lists and dictionaries are configured.
/// </summary>
public class when_binding_collections : given_a_parity_run
{
    protected override IDictionary<string, string?> Configuration => new Dictionary<string, string?>
    {
        ["Cratis:Arc:OnBehalfOf:SigningKeys:0:KeyId"] = "k0",
        ["Cratis:Arc:OnBehalfOf:SigningKeys:0:PublicKeyPem"] = "pem0",
        ["Cratis:Arc:OnBehalfOf:SigningKeys:1:KeyId"] = "k1",
        ["Cratis:Arc:ForwardedIdentity:Enabled"] = "true",
        ["Cratis:Arc:ForwardedIdentity:Keys:0:KeyId"] = "f0",
        ["Cratis:Arc:ForwardedIdentity:Keys:0:SecretBase64"] = "c2VjcmV0",
        ["Cratis:Arc:EndpointGroups:Ports:admin:0"] = "5001",
        ["Cratis:Arc:EndpointGroups:Ports:admin:1"] = "5002",
        ["Cratis:Arc:EndpointGroups:Ports:public:0"] = "not-a-port",
        ["Cratis:Arc:EndpointGroups:RoutePrefixes:admin"] = "/admin"
    };
}
