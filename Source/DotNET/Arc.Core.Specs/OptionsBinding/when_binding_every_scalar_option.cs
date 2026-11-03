// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// Parity of the two binders when every scalar option is configured.
/// </summary>
public class when_binding_every_scalar_option : given_a_parity_run
{
    protected override IDictionary<string, string?> Configuration => new Dictionary<string, string?>
    {
        ["Cratis:Arc:CorrelationId:HttpHeader"] = "x-corr",
        ["Cratis:Arc:Tenancy:ResolverType"] = "Claim",
        ["Cratis:Arc:Tenancy:IngressValidatesTenant"] = "true",
        ["Cratis:Arc:Tenancy:StrictValidatorMembership"] = "true",
        ["Cratis:Arc:Tenancy:HttpHeader"] = "x-tenant",
        ["Cratis:Arc:Tenancy:BaseDomain"] = "example.test",
        ["Cratis:Arc:Tenancy:QueryParameter"] = "tid",
        ["Cratis:Arc:Tenancy:ClaimType"] = "tid",
        ["Cratis:Arc:Tenancy:DevelopmentTenantId"] = "dev",
        ["Cratis:Arc:ConnectionResolution:CacheDuration"] = "00:01:30",
        ["Cratis:Arc:ConnectionResolution:Connection:Resolver"] = "Template",
        ["Cratis:Arc:ConnectionResolution:Connection:Template:ConnectionString"] = "cs",
        ["Cratis:Arc:ConnectionResolution:Identity:Http:Endpoint"] = "https://example.test/x",
        ["Cratis:Arc:ConnectionResolution:Identity:Http:Timeout"] = "00:00:03",
        ["Cratis:Arc:ConnectionResolution:Identity:Http:Headers:a"] = "b",
        ["Cratis:Arc:ConnectionResolution:Identity:Http:Authentication:Type"] = "ClientCredentials",
        ["Cratis:Arc:ConnectionResolution:Identity:Http:Authentication:TokenEndpoint"] = "https://example.test/t",
        ["Cratis:Arc:ConnectionResolution:Identity:Http:Authentication:RefreshMargin"] = "00:02:00",
        ["Cratis:Arc:GeneratedApis:RoutePrefix"] = "v1",
        ["Cratis:Arc:GeneratedApis:SegmentsToSkipForRoute"] = "2",
        ["Cratis:Arc:GeneratedApis:EnableQueryHttpMethod"] = "false",
        ["Cratis:Arc:Introspection:Enabled"] = "false",
        ["Cratis:Arc:Introspection:RequireAuthentication"] = "true",
        ["Cratis:Arc:Introspection:Roles"] = "admin",
        ["Cratis:Arc:TrustForwardedIdentityHeaders"] = "true",
        ["Cratis:Arc:OnBehalfOf:Enabled"] = "true",
        ["Cratis:Arc:OnBehalfOf:Issuer"] = "issuer",
        ["Cratis:Arc:OnBehalfOf:ClockSkew"] = "00:00:10",
        ["Cratis:Arc:Calendar:TimeZone"] = "Europe/Oslo",
        ["Cratis:Arc:Command:MaximumCallerAllowedSeverity"] = "Error",
        ["Cratis:Arc:Query:KeepAliveInterval"] = "00:00:05",
        ["Cratis:Arc:QueryHealth:RequireAuthentication"] = "true",
        ["Cratis:Arc:Hosting:ApplicationUrl"] = "http://127.0.0.1:1/",
        ["Cratis:Arc:ExposeExceptionDetails"] = "true"
    };
}
