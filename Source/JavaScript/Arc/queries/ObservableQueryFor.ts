// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IObservableQueryFor, OnNextResult } from './IObservableQueryFor';
import { ObservableQueryConnection } from './ObservableQueryConnection';
import { ObservableQuerySubscription } from './ObservableQuerySubscription';
import { ValidateRequestArguments } from './ValidateRequestArguments';
import { IObservableQueryConnection } from './IObservableQueryConnection';
import { NullObservableQueryConnection } from './NullObservableQueryConnection';
import { Constructor } from '@cratis/fundamentals';
import { JsonSerializer } from '@cratis/fundamentals';
import { QueryResult } from './QueryResult';
import { Sorting } from './Sorting';
import { Paging } from './Paging';
import { SortDirection } from './SortDirection';
import { Globals } from '../Globals';
import { joinPaths } from '../joinPaths';
import { UrlHelpers } from '../UrlHelpers';
import { GetHttpHeaders } from '../GetHttpHeaders';
import { ParameterDescriptor } from '../reflection/ParameterDescriptor';
import { ParametersHelper } from '../reflection/ParametersHelper';

/* eslint-disable @typescript-eslint/no-explicit-any */

/**
 * Represents an implementation of {@link IQueryFor}.
 * @template TDataType Type of data returned by the query.
 */
export abstract class ObservableQueryFor<TDataType, TParameters = object> implements IObservableQueryFor<TDataType, TParameters> {
    private _microservice: string;
    private _apiBasePath: string;
    private _origin: string;
    private _connection?: IObservableQueryConnection<TDataType>;
    private _httpHeadersCallback: GetHttpHeaders;

    abstract readonly route: string;
    abstract readonly defaultValue: TDataType;
    abstract readonly parameterDescriptors: ParameterDescriptor[];
    abstract get requiredRequestParameters(): string[];
    sorting: Sorting;
    paging: Paging;

    /**
     * Initializes a new instance of the {@link ObservableQueryFor<,>}} class.
     * @param modelType Type of model, if an enumerable, this is the instance type.
     * @param enumerable Whether or not it is an enumerable.
     */
    constructor(readonly modelType: Constructor, readonly enumerable: boolean) {
        this.sorting = Sorting.none;
        this.paging = Paging.noPaging;
        this._microservice = Globals.microservice ?? '';
        this._apiBasePath = Globals.apiBasePath ?? '';
        this._origin = Globals.origin ?? '';
        this._httpHeadersCallback = () => ({});
    }

    /**
     * Disposes the query.
     */
    dispose() {
        this._connection?.disconnect();
    }

    /** @inheritdoc */
    setMicroservice(microservice: string) {
        this._microservice = microservice;
    }

    /** @inheritdoc */
    setApiBasePath(apiBasePath: string): void {
        this._apiBasePath = apiBasePath;
    }

    /** @inheritdoc */
    setOrigin(origin: string): void {
        this._origin = origin;
    }

    /** @inheritdoc */
    setHttpHeadersCallback(callback: GetHttpHeaders): void {
        this._httpHeadersCallback = callback;
    }

    /** @inheritdoc */
    subscribe(callback: OnNextResult<QueryResult<TDataType>>, args?: TParameters): ObservableQuerySubscription<TDataType> {
        if (this._connection) {
            this._connection.disconnect();
        }

        if (!this.validateArguments(args)) {
            this._connection = new NullObservableQueryConnection(this.defaultValue);
        } else {
            const { route } = UrlHelpers.replaceRouteParameters(this.route, args as object);
            const actualRoute = joinPaths(this._apiBasePath, route);
            const url = UrlHelpers.createUrlFrom(this._origin, this._apiBasePath, actualRoute);
            this._connection = new ObservableQueryConnection<TDataType>(url, this._microservice);
        }

        // Build query arguments including unused args parameters, parameter descriptor values, and paging/sorting
        const parameterValues = ParametersHelper.collectParameterValues(this);
        const { unusedParameters } = UrlHelpers.replaceRouteParameters(this.route, args as object);
        const connectionQueryArguments: any = {
            ...unusedParameters,
            ...parameterValues,
            ...this.buildQueryArguments()
        };

        const subscriber = new ObservableQuerySubscription(this._connection);
        this._connection.connect(data => {
            const result: any = data;
            try {
                this.deserializeResult(result);
                callback(result);
            } catch (ex) {
                console.log(ex);
            }
        }, connectionQueryArguments);
        return subscriber;
    }

    /** @inheritdoc */
    async perform(args?: TParameters): Promise<QueryResult<TDataType>> {
        const noSuccess = { ...QueryResult.noSuccess, ...{ data: this.defaultValue } } as QueryResult<TDataType>;

        if (!this.validateArguments(args)) {
            return new Promise<QueryResult<TDataType>>((resolve) => {
                resolve(noSuccess);
            });
        }

        const { route, unusedParameters } = UrlHelpers.replaceRouteParameters(this.route, args as object);
        let actualRoute = joinPaths(this._apiBasePath, route);
        
        const additionalParams: Record<string, string | number> = {};
        if (this.paging.hasPaging) {
            additionalParams.page = this.paging.page;
            additionalParams.pageSize = this.paging.pageSize;
        }

        if (this.sorting.hasSorting) {
            additionalParams.sortBy = this.sorting.field;
            additionalParams.sortDirection = (this.sorting.direction === SortDirection.descending) ? 'desc' : 'asc';
        }

        // Collect parameter values from parameterDescriptors that are set
        const parameterValues = ParametersHelper.collectParameterValues(this);

        const queryParams = UrlHelpers.buildQueryParams({ ...unusedParameters, ...parameterValues }, additionalParams);
        const queryString = queryParams.toString();
        if (queryString) {
            actualRoute += (actualRoute.includes('?') ? '&' : '?') + queryString;
        }

        const url = UrlHelpers.createUrlFrom(this._origin, this._apiBasePath, actualRoute);

        const headers = {
            ...(this._httpHeadersCallback?.() || {}),
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        };

        if (this._microservice?.length > 0) {
            headers[Globals.microserviceHttpHeader] = this._microservice;
        }

        const response = await fetch(url, {
            method: 'GET',
            headers
        });

        try {
            const result = await response.json();
            return new QueryResult(result, this.modelType, this.enumerable);
        } catch {
            return noSuccess;
        }
    }

    private validateArguments(args?: TParameters): boolean {
        return ValidateRequestArguments(this.constructor.name, this.requiredRequestParameters, args as object);
    }

    private buildRoute(args?: TParameters): string {
        const { route } = UrlHelpers.replaceRouteParameters(this.route, args as object);
        const actualRoute = joinPaths(this._apiBasePath, route);
        return actualRoute;
    }

    private buildQueryArguments(): any {
        const queryArguments: any = {};

        if (this.paging && this.paging.pageSize > 0) {
            queryArguments.pageSize = this.paging.pageSize;
            queryArguments.page = this.paging.page;
        }

        if (this.sorting.hasSorting) {
            queryArguments.sortBy = this.sorting.field;
            queryArguments.sortDirection = (this.sorting.direction === SortDirection.descending) ? 'desc' : 'asc';
        }

        return queryArguments;
    }

    private addPagingAndSortingToRoute(route: string): string {
        const additionalParams: Record<string, string | number> = {};
        
        if (this.paging.hasPaging) {
            additionalParams.page = this.paging.page;
            additionalParams.pageSize = this.paging.pageSize;
        }

        if (this.sorting.hasSorting) {
            additionalParams.sortBy = this.sorting.field;
            additionalParams.sortDirection = (this.sorting.direction === SortDirection.descending) ? 'desc' : 'asc';
        }

        const queryParams = UrlHelpers.buildQueryParams({}, additionalParams);
        const queryString = queryParams.toString();
        if (queryString) {
            route += '?' + queryString;
        }
        
        return route;
    }

    private deserializeResult(result: any): void {
        if (this.enumerable) {
            if (Array.isArray(result.data)) {
                result.data = JsonSerializer.deserializeArrayFromInstance(this.modelType, result.data);
            } else {
                result.data = [];
            }
        } else {
            result.data = JsonSerializer.deserializeFromInstance(this.modelType, result.data);
        }
    }
}
