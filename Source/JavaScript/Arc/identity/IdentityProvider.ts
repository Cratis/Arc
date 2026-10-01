// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from '@cratis/fundamentals';
import { IIdentityProvider } from './IIdentityProvider.js';
import { IIdentity } from './IIdentity.js';
import { IdentityProviderResult } from './IdentityProviderResult.js';
import { deserializeIdentityDetails } from './deserializeIdentityDetails.js';
import { GetHttpHeaders } from '../GetHttpHeaders.js';
import { Globals } from '../Globals.js';
import { UrlHelpers } from '../UrlHelpers.js';
import { joinPaths } from '../joinPaths.js';

/**
 * Represents an implementation of {@link IIdentityProvider}.
 *
 * The identity always comes from the `/.cratis/me` endpoint, which derives it from the authenticated request. The
 * result is kept in memory for the page, so repeated calls to {@link IdentityProvider.getCurrent} do not each go to
 * the server; {@link IdentityProvider.refresh} always asks the server again.
*/
export class IdentityProvider extends IIdentityProvider {

    /**
     * The name of the identity cookie earlier versions of Arc wrote and read.
     * @deprecated Arc no longer reads or writes this cookie. The identity comes from the `/.cratis/me` endpoint.
     */
    static readonly CookieName = '.cratis-identity';
    static httpHeadersCallback: GetHttpHeaders | undefined;
    static apiBasePath: string = '';
    static origin: string = '';

    private static cachedResult: Promise<IdentityProviderResult | undefined> | undefined;

    /**
     * Sets the HTTP headers callback.
     * @param callback Callback to set.
     */
    static setHttpHeadersCallback(callback: GetHttpHeaders): void {
        IdentityProvider.httpHeadersCallback = callback;
    }

    /**
     * Sets the API base path.
     * @param apiBasePath API base path to set.
     */
    static setApiBasePath(apiBasePath: string): void {
        if (IdentityProvider.apiBasePath !== apiBasePath) {
            IdentityProvider.clearCache();
        }
        IdentityProvider.apiBasePath = apiBasePath;
    }

    /**
     * Sets the origin.
     * @param origin Origin to set.
     */
    static setOrigin(origin: string): void {
        if (IdentityProvider.origin !== origin) {
            IdentityProvider.clearCache();
        }
        IdentityProvider.origin = origin;
    }

    /**
     * Gets the current identity by optionally specifying the details type.
     * @param type Optional constructor for the details type to enable type-safe deserialization.
     * @returns The current identity as {@link IIdentity}.
     * @remarks The identity is fetched from `/.cratis/me` the first time and kept in memory until
     * {@link IdentityProvider.refresh} or {@link IdentityProvider.clearCache} is called. An identity that could not be
     * resolved is not kept, so the next call asks the server again.
     * The `extends object` constraint is required for compatibility with JsonSerializer.deserializeFromInstance().
     */
    static async getCurrent<TDetails extends object = object>(type?: Constructor<TDetails>): Promise<IIdentity<TDetails>> {
        if (!IdentityProvider.cachedResult) {
            IdentityProvider.fetchAndCache();
        }
        const result = await IdentityProvider.cachedResult;
        return IdentityProvider.toIdentity(result, type);
    }

    /** @inheritdoc */
    async getCurrent<TDetails extends object = object>(type?: Constructor<TDetails>): Promise<IIdentity<TDetails>> {
        return IdentityProvider.getCurrent<TDetails>(type);
    }

    /**
     * Fetches the current identity from `/.cratis/me`, replacing what is kept in memory.
     * @param type Optional constructor for the details type to enable type-safe deserialization.
     * @returns The current identity as {@link IIdentity}.
     */
    static async refresh<TDetails extends object = object>(type?: Constructor<TDetails>): Promise<IIdentity<TDetails>> {
        const result = await IdentityProvider.fetchAndCache();
        return IdentityProvider.toIdentity(result, type);
    }

    /**
     * Forgets the identity kept in memory, so the next {@link IdentityProvider.getCurrent} asks the server again.
     * Call this when the user logs out.
     */
    static clearCache(): void {
        IdentityProvider.cachedResult = undefined;
    }

    /**
     * Forgets the identity kept in memory.
     * @deprecated Arc no longer keeps the identity in a cookie. Use {@link IdentityProvider.clearCache} instead.
     */
    static clearIdentityCookie(): void {
        IdentityProvider.clearCache();
    }

    private static fetchAndCache(): Promise<IdentityProviderResult | undefined> {
        const pending = IdentityProvider.fetchResult();
        IdentityProvider.cachedResult = pending;

        // Only a resolved identity is worth keeping: an unset one, or a failed request, has to be asked for
        // again - otherwise a page that loaded before sign-in would never see the user who signed in.
        const forget = () => {
            if (IdentityProvider.cachedResult === pending) {
                IdentityProvider.cachedResult = undefined;
            }
        };
        pending.then(result => {
            if (!result) forget();
        }, forget);

        return pending;
    }

    private static async fetchResult(): Promise<IdentityProviderResult | undefined> {
        const origin = IdentityProvider.origin || Globals.origin || '';
        const apiBasePath = IdentityProvider.apiBasePath || Globals.apiBasePath || '';
        const route = joinPaths(apiBasePath, '/.cratis/me');
        const url = UrlHelpers.createUrlFrom(origin, apiBasePath, route);
        const response = await fetch(
            url, {
            method: 'GET',
            headers: IdentityProvider.httpHeadersCallback?.() ?? {}
        });

        if (!response.ok) {
            return undefined;
        }

        return await response.json() as IdentityProviderResult;
    }

    private static toIdentity<TDetails extends object = object>(result: IdentityProviderResult | undefined, type?: Constructor<TDetails>): IIdentity<TDetails> {
        if (!result) {
            return IdentityProvider.notSet(type);
        }

        const details = deserializeIdentityDetails(type, result.details);
        return {
            id: result.id,
            name: result.name,
            roles: result.roles || [],
            details: details as TDetails,
            isSet: true,
            isInRole: (role: string) => (result.roles || []).includes(role),
            refresh: () => IdentityProvider.refresh(type)
        };
    }

    private static notSet<TDetails extends object = object>(type?: Constructor<TDetails>): IIdentity<TDetails> {
        return {
            id: '',
            name: '',
            roles: [],
            details: {} as TDetails,
            isSet: false,
            isInRole: () => false,
            refresh: () => IdentityProvider.refresh(type)
        };
    }
}
