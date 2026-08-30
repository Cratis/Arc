// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObservableQueryFor, type QueryResult, type ObservableQuerySubscription, type OnNextResult } from '@cratis/arc/queries';
import type { ParameterDescriptor } from '@cratis/arc/reflection';
import { DateOnly, field, TimeOnly } from '@cratis/fundamentals';

export class FakeCalendarChild {
    @field(DateOnly)
    date!: DateOnly;

    @field(TimeOnly)
    time!: TimeOnly;
}

export class FakeCalendarItem {
    @field(FakeCalendarChild, true)
    children!: FakeCalendarChild[];
}

export type CalendarSubscribeCallback = OnNextResult<QueryResult<FakeCalendarItem[]>>;

export class FakeCalendarObservableQuery extends ObservableQueryFor<FakeCalendarItem[], Record<string, unknown>> {
    readonly route = '/api/fake-calendar-observable-query';
    readonly parameterDescriptors: ParameterDescriptor[] = [];

    get requiredRequestParameters(): string[] {
        return [];
    }

    defaultValue: FakeCalendarItem[] = [];

    constructor() {
        super(FakeCalendarItem, true);
    }

    static subscribeCallbacks: CalendarSubscribeCallback[] = [];
    static subscriptionReturned: ObservableQuerySubscription<FakeCalendarItem[]>;

    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    subscribe(callback: CalendarSubscribeCallback, _queryArguments?: Record<string, unknown>): ObservableQuerySubscription<FakeCalendarItem[]> {
        FakeCalendarObservableQuery.subscribeCallbacks.push(callback);
        // SAFETY: The test double implements the unsubscribe behavior exercised by the hook.
        FakeCalendarObservableQuery.subscriptionReturned = {
            unsubscribe: () => {}
        } as unknown as ObservableQuerySubscription<FakeCalendarItem[]>;
        return FakeCalendarObservableQuery.subscriptionReturned;
    }

    static reset() {
        FakeCalendarObservableQuery.subscribeCallbacks = [];
    }
}
