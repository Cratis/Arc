// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export class UrlHelpers {
    /**
     * Creates a URL from the given origin, API base path, and route.
     * @param origin The origin of the request. If not provided, it defaults to the current document's origin.
     * @param apiBasePath The base path for the API.
     * @param route The specific route for the request.
     * @returns The constructed URL.
    */
    static createUrlFrom(origin: string, apiBasePath: string, route: string): URL {
        if ((!origin || origin.length === 0) && typeof document !== 'undefined') {
            origin = document.location?.origin ?? '';
        }

        return new URL(route, `${origin}${apiBasePath}`);
    }

    /**
     * Replaces route parameters in the route template with values from the parameters object.
     * Returns both the updated route and the parameters that were not used in the route.
     * @param route The route template with placeholders like {paramName}.
     * @param parameters The parameters to replace in the route.
     * @returns An object containing the updated route and unused parameters.
     */
    static replaceRouteParameters<T extends object>(route: string, parameters?: T): { route: string; unusedParameters: Partial<T> } {
        if (!parameters) {
            return { route, unusedParameters: {} };
        }

        let result = route;
        const unusedParameters: Partial<T> = { ...parameters };

        for (const [key, value] of Object.entries(parameters)) {
            // Array values cannot be encoded as a single route segment — leave them as unused so
            // they are serialized as repeated query string parameters (e.g. ?ids=1&ids=2&ids=3).
            if (Array.isArray(value)) {
                continue;
            }

            const pattern = new RegExp(`\\{${key}\\}`, 'gi');
            const newRoute = result.replace(pattern, encodeURIComponent(String(value)));
            
            if (newRoute !== result) {
                delete unusedParameters[key as keyof T];
                result = newRoute;
            }
        }

        return { route: result, unusedParameters };
    }

    /**
     * Builds URLSearchParams from the given parameters and additional query parameters.
     * Array values are serialized as repeated key=value pairs (e.g. ?ids=1&ids=2&ids=3).
     * @param unusedParameters Parameters that were not used in route replacement.
     * @param additionalParams Additional query parameters to include.
     * @returns URLSearchParams containing all parameters.
     */
    static buildQueryParams(unusedParameters: object, additionalParams?: Record<string, string | number>): URLSearchParams {
        const queryParams = new URLSearchParams();

        for (const [key, value] of Object.entries(unusedParameters)) {
            if (value !== undefined && value !== null) {
                if (Array.isArray(value)) {
                    for (const item of value) {
                        queryParams.append(key, String(item));
                    }
                } else {
                    queryParams.set(key, String(value));
                }
            }
        }

        if (additionalParams) {
            for (const [key, value] of Object.entries(additionalParams)) {
                queryParams.set(key, String(value));
            }
        }

        return queryParams;
    }
}