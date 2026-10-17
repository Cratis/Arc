// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, render, RenderResult } from '@testing-library/react';
import sinon from 'sinon';
import { QueryInstanceCache } from '@cratis/arc/queries';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { ArcContext, ArcConfiguration } from '../../../ArcContext.js';
import { QueryInstanceCacheContext } from '../../QueryInstanceCacheContext.js';

/**
 * The paging metadata a response carries when a query is not paged.
 */
const noPaging = { page: 0, size: 0, totalItems: 0, totalPages: 0 };

/**
 * Hooks rendered under one query instance cache and one Arc configuration, with the network replaced by a fetch
 * stub - the world in which hooks for the same query share an instance, its requests and its cached result.
 */
export class a_shared_query_instance_cache {
    readonly arcConfiguration: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com'
    };

    queryCache = new QueryInstanceCache();
    fetchStub!: sinon.SinonStub;
    private readonly fetchHelper = createFetchHelper();

    /**
     * Wraps rendered hooks in the shared query instance cache and the Arc configuration.
     */
    readonly wrapper = ({ children }: { children: React.ReactNode }) =>
        React.createElement(QueryInstanceCacheContext.Provider, { value: this.queryCache },
            React.createElement(ArcContext.Provider, { value: this.arcConfiguration }, children));

    /**
     * Starts a specification from an empty cache and a fresh fetch stub that answers nothing yet.
     */
    establish(): void {
        this.queryCache = new QueryInstanceCache();
        this.fetchStub = this.fetchHelper.stubFetch();
    }

    /**
     * Gives the real fetch back.
     */
    cleanup(): void {
        this.fetchHelper.restore();
    }

    /**
     * Renders an element inside the shared query instance cache and the Arc configuration.
     * @param element The element to render.
     * @returns The render result.
     */
    render(element: React.ReactElement): RenderResult {
        return render(element, { wrapper: this.wrapper });
    }

    /**
     * Creates a successful response carrying the given data.
     * @param data The data of the response.
     * @param paging The paging metadata of the response.
     * @returns The response.
     */
    successfulResponseWith(data: object[], paging: object = noPaging): Response {
        return {
            json: async () => ({
                data,
                isSuccess: true,
                isAuthorized: true,
                isValid: true,
                hasExceptions: false,
                validationResults: [],
                exceptionMessages: [],
                exceptionStackTrace: '',
                paging
            })
        } as Response;
    }

    /**
     * Creates the rejection a fetch produces when its request is aborted.
     * @returns The abort error.
     */
    abortError(): Error {
        return Object.assign(new Error('The operation was aborted'), { name: 'AbortError' });
    }

    /**
     * Holds a request open until its abort signal fires, then rejects it the way fetch does. The abort comes from
     * whatever supersedes the request through the shared query instance - nothing here simulates it.
     * @param requestInit The options the request was made with.
     * @returns A response that never arrives.
     */
    untilAborted(requestInit: RequestInit): Promise<Response> {
        return new Promise<Response>((resolve, reject) => {
            requestInit.signal?.addEventListener('abort', () => reject(this.abortError()));
        });
    }

    /**
     * Gives pending promises a turn to run.
     */
    async nextTurn(): Promise<void> {
        await new Promise(resolve => setTimeout(resolve, 0));
    }

    /**
     * Gives pending promises a turn to run and lets React commit whatever came of it.
     */
    async letPendingWorkSettle(): Promise<void> {
        await act(async () => {
            await this.nextTurn();
        });
    }
}
