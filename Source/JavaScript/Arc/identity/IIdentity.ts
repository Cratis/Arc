// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Defines the context for identity.
 */
export interface IIdentity<TDetails = object> {

    /**
     * The id of the identity.
     */
    id: string;

    /**
     * The name of the identity.
     */
    name: string;

    /**
     * The roles the identity is in.
     */
    roles: string[];

    /**
     * The application specific details for the identity.
     */
    details: TDetails;

    /**
     * Whether the details are set.
     */
    isSet: boolean;

    /**
     * Check if the identity is in a role.
     * @param role The role to check.
     * @returns True if the identity is in the role, false otherwise.
     */
    isInRole(role: string): boolean;

    /**
     * Refreshes the identity context.
     */
    refresh(): Promise<IIdentity<TDetails>>;
}
