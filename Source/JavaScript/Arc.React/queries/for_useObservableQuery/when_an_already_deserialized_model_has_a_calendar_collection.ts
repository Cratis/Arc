// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, render } from '@testing-library/react';
import { DateOnly, TimeOnly } from '@cratis/fundamentals';
import { QueryInstanceCache, type QueryResult, type QueryResultWithState } from '@cratis/arc/queries';
import { useObservableQuery } from '../useObservableQuery';
import { FakeCalendarChild, FakeCalendarItem, FakeCalendarObservableQuery } from './FakeCalendarObservableQuery';
import { type ArcConfiguration, ArcContext } from '../../ArcContext';
import { QueryInstanceCacheContext } from '../QueryInstanceCacheContext';

describe('when an already deserialized model has a calendar collection', () => {
    let calendarDate: DateOnly;
    let calendarTime: TimeOnly;
    let capturedResult: QueryResultWithState<FakeCalendarItem[]> | undefined;
    let cachedResult: QueryResultWithState<FakeCalendarItem[]> | undefined;

    beforeEach(async () => {
        FakeCalendarObservableQuery.reset();
        calendarDate = DateOnly.from(2026, 2, 3);
        calendarTime = TimeOnly.from(14, 15, 16);
        capturedResult = undefined;
        cachedResult = undefined;

        const queryCache = new QueryInstanceCache();
        const configuration: ArcConfiguration = {
            microservice: 'test-microservice',
            apiBasePath: '/api',
            origin: 'https://example.com',
        };

        const TestComponent = () => {
            const [result] = useObservableQuery<FakeCalendarItem[], FakeCalendarObservableQuery>(FakeCalendarObservableQuery);
            capturedResult = result;
            return React.createElement('div', null, 'Test');
        };

        render(
            React.createElement(
                QueryInstanceCacheContext.Provider,
                { value: queryCache },
                React.createElement(
                    ArcContext.Provider,
                    { value: configuration },
                    React.createElement(TestComponent)
                )
            )
        );

        const child = new FakeCalendarChild();
        child.date = calendarDate;
        child.time = calendarTime;
        const item = new FakeCalendarItem();
        item.children = [child];
        const callback = FakeCalendarObservableQuery.subscribeCallbacks[0];

        await act(async () => {
            // SAFETY: The test payload implements the query-result contract while preserving live model instances.
            callback({
                data: [item],
                isSuccess: true,
                isAuthorized: true,
                isValid: true,
                hasExceptions: false,
                validationResults: [],
                exceptionMessages: [],
                exceptionStackTrace: '',
                paging: { page: 0, size: 0, totalItems: 1, totalPages: 1 }
            } as unknown as QueryResult<FakeCalendarItem[]>);
        });

        const cacheKey = queryCache.buildKey(FakeCalendarObservableQuery.name);
        cachedResult = queryCache.getLastResult<FakeCalendarItem[]>(cacheKey);
    });

    it('should emit the live child date', () => capturedResult!.data[0].children[0].date.should.equal(calendarDate));
    it('should emit the live child time', () => capturedResult!.data[0].children[0].time.should.equal(calendarTime));
    it('should cache the live child date', () => cachedResult!.data[0].children[0].date.should.equal(calendarDate));
    it('should cache the live child time', () => cachedResult!.data[0].children[0].time.should.equal(calendarTime));
});
