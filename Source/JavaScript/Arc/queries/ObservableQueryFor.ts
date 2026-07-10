// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IObservableQueryFor, OnNextResult } from './IObservableQueryFor';
import { ObservableQuerySubscription } from './ObservableQuerySubscription';
import { ValidateRequestArguments } from './ValidateRequestArguments';
import { IObservableQueryConnection } from './IObservableQueryConnection';
import { NullObservableQueryConnection } from './NullObservableQueryConnection';
import { createObservableQueryConnection } from './ObservableQueryConnectionFactory';
import { Constructor } from '@cratis/fundamentals';
import { JsonSerializer } from '@cratis/fundamentals';
import { QueryResult } from './QueryResult';
import { Sorting } from './Sorting';
import { Paging } from './Paging';
import { SortDirection } from './SortDirection';
import { Globals } from '../Globals';
import { UrlHelpers } from '../UrlHelpers';
import { GetHttpHeaders } from '../GetHttpHeaders';
import { ParameterDescriptor } from '../reflection/ParameterDescriptor';
import { ParametersHelper } from '../reflection/ParametersHelper';
import { QueryHttpMethod } from './QueryHttpMethod';
import { executeQueryHttpRequest } from './QueryHttpRequest';

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
    private _httpMethod?: QueryHttpMethod;

    abstract readonly route: string;
    abstract readonly defaultValue: TDataType;
    readonly roles: string[] = [];
    /** Backend fully-qualified query name used when subscribing via the SSE hub. Overridden in generated proxies. */
    readonly queryName?: string;
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
    setHttpMethod(method: QueryHttpMethod): void {
        this._httpMethod = method;
    }

    /** @inheritdoc */
    subscribe(callback: OnNextResult<QueryResult<TDataType>>, args?: TParameters): ObservableQuerySubscription<TDataType> {
        if (this._connection) {
            this._connection.disconnect();
        }

        if (!this.validateArguments(args)) {
            this._connection = new NullObservableQueryConnection(this.defaultValue);
        } else {
            this._connection = createObservableQueryConnection({
                route: this.route,
                queryName: this.queryName ?? this.constructor.name,
                origin: this._origin,
                apiBasePath: this._apiBasePath,
                microservice: this._microservice,
                args: args as object,
            });
        }

        // Descriptor-backed instance properties provide defaults; fresh args passed to subscribe()
        // must take precedence over any stale instance property values. Spread parameterValues
        // first so that the caller-supplied args can override them.
        //
        // In direct mode the route arguments are already embedded in the URL path, so only
        // the unused (non-route) parameters are appended as additional query arguments.
        // In multiplexed mode ALL arguments — including route-derived ones — must be included
        // in the subscribe payload so the server can execute the query correctly.
        const parameterValues = ParametersHelper.collectParameterValues(this);
        const { unusedParameters } = UrlHelpers.replaceRouteParameters(this.route, args as object);
        const connectionQueryArguments: any = {
            ...parameterValues,
            ...(Globals.queryDirectMode ? unusedParameters : (args as object) || {}),
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

        // Collect parameter values from parameterDescriptors that are set
        const parameterValues = ParametersHelper.collectParameterValues(this);

        const headers = {
            ...(this._httpHeadersCallback?.() || {}),
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        };

        if (this._microservice?.length > 0) {
            headers[Globals.microserviceHttpHeader] = this._microservice;
        }

        const response = await executeQueryHttpRequest(this._httpMethod ?? Globals.queryHttpMethod, {
            route: this.route,
            apiBasePath: this._apiBasePath,
            origin: this._origin,
            args: (args as object) ?? {},
            parameterValues,
            paging: this.paging,
            sorting: this.sorting,
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
        const parameterValues = ParametersHelper.collectParameterValues(this);
        const combinedArgs = { ...(args as object || {}), ...parameterValues };
        return ValidateRequestArguments(this.constructor.name, this.requiredRequestParameters, combinedArgs as object);
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

    private deserializeResult(result: any): void {
        if (this.enumerable) {
            if (Array.isArray(result.data)) {
                result.data = JsonSerializer.deserializeArrayFromInstance(this.modelType, result.data);
            } else {
                result.data = [];
            }
        } else if (result.data) {
            result.data = JsonSerializer.deserializeFromInstance(this.modelType, result.data);
        }
    }
}
