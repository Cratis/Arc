// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { useIdentity } from './useIdentity';
import { IIdentityContext } from './IIdentityContext';

/**
 * Predicate deciding whether an authenticated identity is allowed.
 *
 * The details are declared as possibly absent because they genuinely are: an identity is set as soon
 * as the server answers, whether or not the application registered anything to fill details in. A
 * predicate written as if they were always there throws on the applications that have none.
 * {@link RequireRole} never calls a predicate with absent details - it denies first - but the type
 * keeps the parameter honest for predicates that are reused outside the gate.
 * @typeparam TDetails Type of the details carried by the identity.
 */
export type IdentityPredicate<TDetails = object> = (details: TDetails | undefined, identity: IIdentityContext<TDetails>) => boolean;

/**
 * The slots every {@link RequireRole} configuration renders into.
 */
export interface RequireRoleSlots {
    /**
     * Rendered when the identity is authenticated and allowed.
     */
    children: React.ReactNode;

    /**
     * Rendered while the identity is still being resolved. Defaults to nothing.
     *
     * This covers every resolution, not just the first one - a refresh after signing in or being
     * granted a role re-enters loading as well.
     */
    whileLoading?: React.ReactNode;

    /**
     * Rendered when the caller is anonymous, or is authenticated but not allowed. Defaults to nothing.
     *
     * This is a slot rather than a redirect on purpose - Arc.React does not depend on a router, so an
     * application that wants to redirect passes its own navigation element here.
     */
    forbidden?: React.ReactNode;
}

/**
 * Access expressed as roles, optionally narrowed further by a predicate.
 * @typeparam TDetails Type of the details carried by the identity.
 */
export interface RequireRoleByRoles<TDetails = object> {
    /**
     * Roles that grant access - the identity needs any one of them.
     *
     * An empty array denies everyone: no role can be matched, so no caller gets through. Anything
     * that is not an array is a misconfiguration and is denied as well.
     */
    roles: string[];

    /**
     * Optional predicate that must also pass. See {@link RequireRoleByPredicate.allow}.
     */
    allow?: IdentityPredicate<TDetails>;
}

/**
 * Access expressed as a predicate, optionally narrowed further by roles.
 * @typeparam TDetails Type of the details carried by the identity.
 */
export interface RequireRoleByPredicate<TDetails = object> {
    /**
     * Optional roles the identity needs any one of. See {@link RequireRoleByRoles.roles}.
     */
    roles?: string[];

    /**
     * Predicate deciding access from the identity's details.
     *
     * Details come first because that is what an application's own access rules are written against;
     * the whole identity is passed as a second argument for rules that need
     * {@link IIdentityContext.isInRole}. A predicate that throws denies - a gate that cannot reach a
     * decision is not a gate that opens.
     *
     * Pass `allow={() => true}` to gate on authentication alone.
     */
    allow: IdentityPredicate<TDetails>;
}

/**
 * Props for {@link RequireRole}.
 *
 * At least one of {@link RequireRoleByRoles.roles} and {@link RequireRoleByPredicate.allow} has to be
 * supplied - a gate with no access rule has nothing to decide with, so it denies. Gating on
 * authentication alone is spelled out explicitly as `allow={() => true}`.
 * @typeparam TDetails Type of the details carried by the identity.
 */
export type RequireRoleProps<TDetails = object> = RequireRoleSlots & (RequireRoleByRoles<TDetails> | RequireRoleByPredicate<TDetails>);

/**
 * Renders its children only for an identity that is authenticated and allowed.
 *
 * Access is expressed as {@link RequireRoleByRoles.roles}, {@link RequireRoleByPredicate.allow}, or
 * both - when both are supplied both must pass. Every path that is not an unambiguous "yes" renders
 * {@link RequireRoleSlots.forbidden}: an anonymous caller, a failed role or predicate check, a
 * predicate that throws, absent details, and a gate configured with no access rule at all.
 *
 * The three outcomes are kept apart deliberately: a caller whose identity has not arrived yet is not
 * the same as one who is anonymous, and treating the two alike makes a signed-in user flash the
 * forbidden state on every load. {@link IIdentityContext.isLoading} is what separates them.
 *
 * **This hides UI, it does not protect data.** The identity it reads comes from a cookie that is
 * deliberately not `HttpOnly`, so the browser - and anyone using it - can edit it and render these
 * children at will. Treat the gate as a way to keep people out of screens that would only frustrate
 * them, never as the thing that keeps them out of the data: every query and command behind it has to
 * carry its own `[Authorize]`/`[Roles]` on the server, where the decision cannot be edited.
 * @typeparam TDetails Type of the details carried by the identity.
 * @param props The {@link RequireRoleProps}.
 * @returns The rendered outcome for the current identity.
 */
export function RequireRole<TDetails = object>(props: RequireRoleProps<TDetails>): React.ReactElement {
    const identity = useIdentity<TDetails>();
    const forbidden = <>{props.forbidden ?? null}</>;

    // Configuration is decided before the identity is, because no identity can change the answer.
    // `undefined` is what a renamed configuration key or a missing feature flag looks like, and it is
    // the shape most likely to reach here by accident - so it denies rather than admits.
    if (props.roles === undefined && props.allow === undefined) {
        console.warn('RequireRole was given neither `roles` nor `allow`, so it has no access rule to apply and denies everyone. Pass the roles or the predicate that were meant to be there, or `allow={() => true}` if authentication alone is the rule.');
        return forbidden;
    }

    if (props.roles !== undefined && !Array.isArray(props.roles)) {
        console.warn('RequireRole was given a `roles` that is not an array, so it cannot be matched against and denies everyone. Check where the value comes from - configuration and feature flags are the usual source.', props.roles);
        return forbidden;
    }

    if (identity.isLoading) {
        return <>{props.whileLoading ?? null}</>;
    }

    if (!identity.isSet) {
        return forbidden;
    }

    if (props.roles !== undefined && !props.roles.some(role => identity.isInRole(role))) {
        return forbidden;
    }

    if (props.allow !== undefined && !isAllowedBy(props.allow, identity)) {
        return forbidden;
    }

    return <>{props.children}</>;
}

/**
 * Applies a predicate to an identity, treating everything except an unambiguous `true` as a denial.
 * @param allow The predicate to apply.
 * @param identity The identity to decide on.
 * @returns True when the predicate allowed the identity, false otherwise.
 */
function isAllowedBy<TDetails>(allow: IdentityPredicate<TDetails>, identity: IIdentityContext<TDetails>): boolean {
    if (identity.details === undefined) {
        // The two natural ways to phrase a predicate disagree about absent details - one throws, the
        // other reads the absence as innocence and admits - so the gate answers instead of letting
        // the phrasing decide.
        console.warn('RequireRole has an `allow` predicate but the identity carries no details, so there is nothing to decide on and access is denied. The server answered without details - check that an IProvideIdentityDetails implementation is registered.');
        return false;
    }

    try {
        return allow(identity.details, identity) === true;
    } catch (error) {
        console.warn('RequireRole denied access because the `allow` predicate threw. A predicate that cannot reach a decision is not a decision to let the caller through.', error);
        return false;
    }
}
