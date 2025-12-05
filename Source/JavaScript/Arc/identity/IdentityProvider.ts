// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IIdentityProvider } from './IIdentityProvider';
import { IIdentity } from './IIdentity';
import { IdentityProviderResult } from './IdentityProviderResult';
import { GetHttpHeaders } from 'GetHttpHeaders';
import { Globals } from '../Globals';
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
     * @returns The current identity as {@link IIdentity}.
     */
    static async getCurrent<TDetails = object>(): Promise<IIdentity<TDetails>> {
        const cookie = this.getCookie();
        if (cookie.length == 2) {
            const json = atob(cookie[1]);
            const result = JSON.parse(json) as IdentityProviderResult;
            return {
                id: result.id,
                name: result.name,
                details: result.details,
                isSet: true,
                refresh: IdentityProvider.refresh
            } as IIdentity<TDetails>;
        } else {
            const identity = await this.refresh<TDetails>();
            return identity;
        }
    }

    /** @inheritdoc */
    async getCurrent<TDetails = object>(): Promise<IIdentity<TDetails>> {
        return IdentityProvider.getCurrent<TDetails>();
    }

    static async refresh<TDetails = object>(): Promise<IIdentity<TDetails>> {
        IdentityProvider.clearCookie();
        const apiBasePath = IdentityProvider.apiBasePath || Globals.apiBasePath || '';
        const url = joinPaths(apiBasePath, '/.cratis/me');
        const response = await fetch(
            url, {
            method: 'GET',
            headers: IdentityProvider.httpHeadersCallback?.() ?? {}
        });

        const result = await response.json() as IdentityProviderResult;

        return {
            id: result.id,
            name: result.name,
            details: result.details as TDetails,
            isSet: true,
            refresh: IdentityProvider.refresh
        };
    }

    private static getCookie() {
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
        document.cookie = `${IdentityProvider.CookieName}=;expires=Thu, 01 Jan 1970 00:00:00 GMT`;
    }
}
