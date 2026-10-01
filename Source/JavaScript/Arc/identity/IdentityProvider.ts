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
 * The identity comes from the `/.cratis/me` endpoint, which derives it from the authenticated request. The
 * result is kept in memory for the page, so repeated calls to {@link IdentityProvider.getCurrent} do not each go to
 * the server; {@link IdentityProvider.refresh} always asks the server again.
 *
 * Only when the server has no `/.cratis/me` endpoint (it answers 404) does it fall back to the readable
 * `.cratis-identity` cookie earlier versions relied on, and it warns when it does. That fallback is for the transition
 * and will be removed.
*/
export class IdentityProvider extends IIdentityProvider {

    /**
     * The name of the identity cookie earlier versions of Arc wrote and read.
     * @deprecated The identity comes from the `/.cratis/me` endpoint. The cookie is only read when that endpoint does
     * not exist, and that fallback will be removed.
     */
    static readonly CookieName = '.cratis-identity';
    static httpHeadersCallback: GetHttpHeaders | undefined;
    static apiBasePath: string = '';
    static origin: string = '';

    private static cachedResult: Promise<IdentityProviderResult | undefined> | undefined;
    private static hasWarnedAboutLegacyCookie = false;

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
            IdentityProvider.cachedResult = undefined;
        }
        IdentityProvider.apiBasePath = apiBasePath;
    }

    /**
     * Sets the origin.
     * @param origin Origin to set.
     */
    static setOrigin(origin: string): void {
        if (IdentityProvider.origin !== origin) {
            IdentityProvider.cachedResult = undefined;
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
        // Earlier backends resolve the cookie before consulting the current credentials. Keep a snapshot only
        // for the 404 transition fallback, but do not send the stale cookie with an explicit refresh request.
        const legacyCookies = typeof document === 'undefined' ? '' : document.cookie;
        IdentityProvider.clearCache();
        const result = await IdentityProvider.fetchAndCache(legacyCookies);
        return IdentityProvider.toIdentity(result, type);
    }

    /**
     * Forgets the identity kept in memory, so the next {@link IdentityProvider.getCurrent} asks the server again.
     * It also expires a readable `.cratis-identity` cookie left by an earlier version. Call this when the user logs out.
     */
    static clearCache(): void {
        IdentityProvider.cachedResult = undefined;
        if (typeof document === 'undefined') return;
        document.cookie = `${IdentityProvider.CookieName}=;expires=Thu, 01 Jan 1970 00:00:00 GMT;path=/`;
    }

    /**
     * Forgets the identity kept in memory.
     * @deprecated Arc no longer keeps the identity in a cookie. Use {@link IdentityProvider.clearCache} instead.
     */
    static clearIdentityCookie(): void {
        IdentityProvider.clearCache();
    }

    private static fetchAndCache(legacyCookies?: string): Promise<IdentityProviderResult | undefined> {
        const pending = IdentityProvider.fetchResult(legacyCookies);
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

    private static async fetchResult(legacyCookies?: string): Promise<IdentityProviderResult | undefined> {
        const origin = IdentityProvider.origin || Globals.origin || '';
        const apiBasePath = IdentityProvider.apiBasePath || Globals.apiBasePath || '';
        const route = joinPaths(apiBasePath, '/.cratis/me');
        const url = UrlHelpers.createUrlFrom(origin, apiBasePath, route);
        const response = await fetch(
            url, {
            method: 'GET',
            headers: IdentityProvider.httpHeadersCallback?.() ?? {}
        });

        // No identity endpoint at all - an application that has not mapped one, or one whose identity comes from a
        // proxy in front of it. Anything else, a 401 or 403 above all, is the server's answer and a cookie the
        // browser holds must never overrule it.
        if (response.status === 404) {
            return IdentityProvider.fromLegacyCookie(legacyCookies);
        }

        if (!response.ok) {
            return undefined;
        }

        return await response.json() as IdentityProviderResult;
    }

    private static fromLegacyCookie(legacyCookies?: string): IdentityProviderResult | undefined {
        if (typeof document === 'undefined') return undefined;
        const prefix = `${IdentityProvider.CookieName}=`;
        const cookie = (legacyCookies ?? document.cookie).split(';').map(_ => _.trim()).find(_ => _.startsWith(prefix));
        if (!cookie) return undefined;

        try {
            const result = JSON.parse(atob(decodeURIComponent(cookie.substring(prefix.length)))) as IdentityProviderResult;
            if (!IdentityProvider.hasWarnedAboutLegacyCookie) {
                IdentityProvider.hasWarnedAboutLegacyCookie = true;
                console.warn(
                    `The server has no '/.cratis/me' endpoint, so the identity was read from the '${IdentityProvider.CookieName}' cookie. ` +
                    'That cookie is not protected and the fallback will be removed in a future major version. ' +
                    'Expose /.cratis/me to the frontend: https://github.com/Cratis/Arc/blob/main/Documentation/backend/csharp/identity/migrating-from-the-identity-cookie.md');
            }
            return result;
        } catch {
            return undefined;
        }
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
