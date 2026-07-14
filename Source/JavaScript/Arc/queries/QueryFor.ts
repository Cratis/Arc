// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IQueryFor } from './IQueryFor';
import { QueryResult } from "./QueryResult";
import { QueryValidator } from './QueryValidator';
import { ValidateRequestArguments } from './ValidateRequestArguments';
import { Constructor } from '@cratis/fundamentals';
import { Paging } from './Paging';
import { Globals } from '../Globals';
import { Sorting } from './Sorting';
import { GetHttpHeaders } from '../GetHttpHeaders';
import { ParameterDescriptor } from '../reflection/ParameterDescriptor';
import { ParametersHelper } from '../reflection/ParametersHelper';
import { QueryHttpMethod } from './QueryHttpMethod';
import { executeQueryHttpRequest } from './QueryHttpRequest';

/**
 * Represents an implementation of {@link IQueryFor}.
 * @template TDataType Type of data returned by the query.
 */
export abstract class QueryFor<TDataType, TParameters = object> implements IQueryFor<TDataType, TParameters> {
    private _microservice: string;
    private _apiBasePath: string;
    private _origin: string;
    private _httpHeadersCallback: GetHttpHeaders;
    private _httpMethod?: QueryHttpMethod;
    abstract readonly route: string;
    /** Backend fully-qualified query name used as cache key. Overridden in generated proxies. */
    readonly queryName?: string;
    /* eslint-disable @typescript-eslint/no-explicit-any */
    readonly validation?: QueryValidator<any>;
    /* eslint-enable @typescript-eslint/no-explicit-any */
    readonly roles: string[] = [];
    abstract readonly parameterDescriptors: ParameterDescriptor[];
    abstract get requiredRequestParameters(): string[];
    abstract defaultValue: TDataType;
    abortController?: AbortController;
    sorting: Sorting;
    paging: Paging;
    parameters: TParameters | undefined;

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
    async perform(args?: TParameters): Promise<QueryResult<TDataType>> {
        const noSuccess = { ...QueryResult.noSuccess, ...{ data: this.defaultValue } } as QueryResult<TDataType>;

        args = args || this.parameters;

        const clientValidationErrors = this.validation?.validate(args as object || {}) || [];
        if (clientValidationErrors.length > 0) {
            return new QueryResult({
                data: this.defaultValue as object,
                isSuccess: false,
                isAuthorized: true,
                isValid: false,
                hasExceptions: false,
                validationResults: clientValidationErrors.map(_ => ({
                    severity: _.severity,
                    message: _.message,
                    members: _.members,
                    state: _.state
                })),
                exceptionMessages: [],
                exceptionStackTrace: '',
                paging: {
                    totalItems: 0,
                    totalPages: 0,
                    page: 0,
                    size: 0
                }
            }, this.modelType, this.enumerable) as QueryResult<TDataType>;
        }

        if (!ValidateRequestArguments(this.constructor.name, this.requiredRequestParameters, args as object)) {
            return new Promise<QueryResult<TDataType>>((resolve) => {
                resolve(noSuccess);
            });
        }

        if (this.abortController) {
            this.abortController.abort();
        }

        this.abortController = new AbortController();

        // Collect parameter values from parameterDescriptors that are set
        const parameterValues = ParametersHelper.collectParameterValues(this);

        const headers = {
            ... this._httpHeadersCallback?.(), ...
            {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            }
        };

        if (this._microservice?.length > 0) {
            headers[Globals.microserviceHttpHeader] = this._microservice;
        }

        const response = await executeQueryHttpRequest(this._httpMethod, {
            route: this.route,
            apiBasePath: this._apiBasePath,
            origin: this._origin,
            args: (args as object) ?? {},
            parameterValues,
            paging: this.paging,
            sorting: this.sorting,
            headers,
            signal: this.abortController.signal
        });

        try {
            const result = await response.json();
            return new QueryResult(result, this.modelType, this.enumerable);
        } catch {
            return noSuccess;
        }
    }
}
