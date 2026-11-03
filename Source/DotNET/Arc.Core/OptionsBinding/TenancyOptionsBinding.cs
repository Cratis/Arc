// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// The binding table of <see cref="TenancyOptions"/>.
/// </summary>
internal static class TenancyOptionsBinding
{
    /// <summary>
    /// Gets how <see cref="TenancyOptions"/> is bound.
    /// </summary>
    internal static readonly ObjectType Bound = new(typeof(TenancyOptions), static () => new TenancyOptions(), Members);

    static Member[] Members() =>
    [
        Member.Of<TenancyOptions, TenantResolverType>("ResolverType", Leaves.TenantResolverType, static _ => _.ResolverType, static (o, v) => o.ResolverType = v),
        Member.Of<TenancyOptions, bool>("IngressValidatesTenant", Leaves.Bool, static _ => _.IngressValidatesTenant, static (o, v) => o.IngressValidatesTenant = v),
        Member.Of<TenancyOptions, bool>("StrictValidatorMembership", Leaves.Bool, static _ => _.StrictValidatorMembership, static (o, v) => o.StrictValidatorMembership = v),
        Member.Of<TenancyOptions, string>("HttpHeader", Leaves.String, static _ => _.HttpHeader, static (o, v) => o.HttpHeader = v),
        Member.Of<TenancyOptions, string>("BaseDomain", Leaves.String, static _ => _.BaseDomain, static (o, v) => o.BaseDomain = v),
        Member.Of<TenancyOptions, string>("QueryParameter", Leaves.String, static _ => _.QueryParameter, static (o, v) => o.QueryParameter = v),
        Member.Of<TenancyOptions, string>("ClaimType", Leaves.String, static _ => _.ClaimType, static (o, v) => o.ClaimType = v),
        Member.Of<TenancyOptions, string>("FixedTenantId", Leaves.String, static _ => _.FixedTenantId, static (o, v) => o.FixedTenantId = v),
        Member.Of<TenancyOptions, string>("DevelopmentTenantId", Leaves.String, static _ => _.DevelopmentTenantId, static (o, v) => o.DevelopmentTenantId = v)
    ];
}
