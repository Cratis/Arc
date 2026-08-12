// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { useIdentity } from './useIdentity';
import { IIdentityContext } from './IIdentityContext';

/**
 * Predicate deciding whether an authenticated identity is allowed.
 * @typeparam TDetails Type of the details carried by the identity.
 */
export type IdentityPredicate<TDetails = object> = (details: TDetails, identity: IIdentityContext<TDetails>) => boolean;

/**
 * Props for {@link RequireRole}.
 * @typeparam TDetails Type of the details carried by the identity.
 */
export interface RequireRoleProps<TDetails = object> {
    /**
     * Roles that grant access - the identity needs any one of them.
     *
     * Leaving this out means membership is not consulted at all; it does not mean "no roles allowed".
     */
    roles?: string[];

    /**
     * Predicate deciding access from the identity's details.
     *
     * Details come first because that is what an application's own access rules are written against;
     * the whole identity is passed as a second argument for rules that need {@link IIdentityContext.isInRole}.
     */
    allow?: IdentityPredicate<TDetails>;

    /**
     * Rendered when the identity is authenticated and allowed.
     */
    children: React.ReactNode;

    /**
     * Rendered while the identity is still being resolved. Defaults to nothing.
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
 * Renders its children only for an identity that is authenticated and allowed.
 *
 * Access is expressed as {@link RequireRoleProps.roles}, {@link RequireRoleProps.allow}, or both -
 * when both are supplied both must pass. Supplying neither gates on authentication alone.
 *
 * The three outcomes are kept apart deliberately: a caller whose identity has not arrived yet is not
 * the same as one who is anonymous, and treating the two alike makes a signed-in user flash the
 * forbidden state on every load. {@link IIdentityContext.isLoading} is what separates them.
 * @typeparam TDetails Type of the details carried by the identity.
 * @param props The {@link RequireRoleProps}.
 * @returns The rendered outcome for the current identity.
 */
export function RequireRole<TDetails = object>(props: RequireRoleProps<TDetails>): React.ReactElement {
    const identity = useIdentity<TDetails>();

    if (identity.isLoading) {
        return <>{props.whileLoading ?? null}</>;
    }

    if (!identity.isSet) {
        return <>{props.forbidden ?? null}</>;
    }

    const isInAnyRole = props.roles === undefined || props.roles.some(role => identity.isInRole(role));
    const isAllowed = props.allow === undefined || props.allow(identity.details, identity);

    if (!isInAnyRole || !isAllowed) {
        return <>{props.forbidden ?? null}</>;
    }

    return <>{props.children}</>;
}
