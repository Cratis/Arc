// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor, JsonSerializer } from '@cratis/fundamentals';
import { IIdentityProvider } from './IIdentityProvider';
import { IIdentity } from './IIdentity';
import { IdentityProviderResult } from './IdentityProviderResult';
import { GetHttpHeaders } from 'GetHttpHeaders';
import { Globals } from '../Globals';
import { UrlHelpers } from '../UrlHelpers';
import { joinPaths } from '../joinPaths';

/**
 * Represents an implementation of {@link IIdentityProvider}.
*/
export class IdentityProvider extends IIdentityProvider {

    static readonly CookieName = '.cratis-identity';
    static httpHeadersCallback: GetHttpHeaders | undefined;
    static apiBasePath: string = '';
    static origin: string = '';

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
        IdentityProvider.apiBasePath = apiBasePath;
    }

    /**
     * Sets the origin.
     * @param origin Origin to set.
     */
    static setOrigin(origin: string): void {
        IdentityProvider.origin = origin;
    }

    /**
     * Gets the current identity by optionally specifying the details type.
     * @param type Optional constructor for the details type to enable type-safe deserialization.
     * @returns The current identity as {@link IIdentity}.
     * @remarks The `extends object` constraint is required for compatibility with JsonSerializer.deserializeFromInstance().
     */
    static async getCurrent<TDetails extends object = object>(type?: Constructor<TDetails>): Promise<IIdentity<TDetails>> {
        const cookie = this.getCookie();
        if (cookie.length == 2) {
            const json = atob(cookie[1]);
            const result = JSON.parse(json) as IdentityProviderResult;
            const details = type ? JsonSerializer.deserializeFromInstance(type, result.details) : result.details;
            return {
                id: result.id,
                name: result.name,
                details: details as TDetails,
                isSet: true,
                refresh: () => IdentityProvider.refresh(type)
            } as IIdentity<TDetails>;
        } else {
            const identity = await this.refresh<TDetails>(type);
            return identity;
        }
    }

    /** @inheritdoc */
    async getCurrent<TDetails extends object = object>(type?: Constructor<TDetails>): Promise<IIdentity<TDetails>> {
        return IdentityProvider.getCurrent<TDetails>(type);
    }

    static async refresh<TDetails extends object = object>(type?: Constructor<TDetails>): Promise<IIdentity<TDetails>> {
        IdentityProvider.clearCookie();
        const origin = IdentityProvider.origin || Globals.origin || '';
        const apiBasePath = IdentityProvider.apiBasePath || Globals.apiBasePath || '';
        const route = joinPaths(apiBasePath, '/.cratis/me');
        const url = UrlHelpers.createUrlFrom(origin, apiBasePath, route);
        const response = await fetch(
            url, {
            method: 'GET',
            headers: IdentityProvider.httpHeadersCallback?.() ?? {}
        });

        const result = await response.json() as IdentityProviderResult;
        const details = type ? JsonSerializer.deserializeFromInstance(type, result.details) : result.details;

        return {
            id: result.id,
            name: result.name,
            details: details as TDetails,
            isSet: true,
            refresh: () => IdentityProvider.refresh(type)
        };
    }

    private static getCookie() {
        if (typeof document === 'undefined') return [];
        const decoded = decodeURIComponent(document.cookie);
        const cookies = decoded.split(';').map(_ => _.trim());
        const cookie = cookies.find(_ => _.indexOf(`${IdentityProvider.CookieName}=`) == 0);
        if (cookie) {
            const keyValue = cookie.split('=');
            return [keyValue[0].trim(), keyValue[1].trim()];
        }
        return [];
    }

    private static clearCookie() {
        if (typeof document === 'undefined') return;
        document.cookie = `${IdentityProvider.CookieName}=;expires=Thu, 01 Jan 1970 00:00:00 GMT`;
    }
}
