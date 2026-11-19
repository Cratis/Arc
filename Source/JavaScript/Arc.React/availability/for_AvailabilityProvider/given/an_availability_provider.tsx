// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, render, RenderResult } from '@testing-library/react';
import { AvailableParts, UnavailablePart } from '@cratis/arc/availability';
import { QueryResult } from '@cratis/arc/queries';
import { an_observable_query_cache } from '../../../queries/for_useObservableQuery/given/an_observable_query_cache.js';
import { AvailabilityProvider } from '../../AvailabilityProvider.js';

export class an_availability_provider extends an_observable_query_cache {
    rendered!: RenderResult;

    show(content: React.ReactNode): void {
        this.rendered = render(React.createElement(AvailabilityProvider, null, content), { wrapper: this.wrapper });
    }

    async pushUnavailable(...unavailable: UnavailablePart[]): Promise<void> {
        const data = new AvailableParts();
        data.unavailable = unavailable as never;
        await this.pushResult({ data, isSuccess: true, isAuthorized: true });
    }

    async pushFailure(): Promise<void> {
        await this.pushResult({ data: new AvailableParts(), isSuccess: false, isAuthorized: true, hasExceptions: true, isRetryable: true });
    }

    private async pushResult(result: Record<string, unknown>): Promise<void> {
        const callback = this.connectStub.lastCall.args[0] as (result: QueryResult<AvailableParts>) => void;
        await act(async () => callback({
            hasData: true,
            paging: { page: 0, size: 0, totalItems: 0, totalPages: 1 },
            isValid: true,
            hasExceptions: false,
            validationResults: [],
            exceptionMessages: [],
            exceptionStackTrace: '',
            isReady: true,
            ...result
        } as unknown as QueryResult<AvailableParts>));
    }
}
