// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    ObservableQueryFor,
    type QueryResult,
    type ObservableQuerySubscription,
    type OnNextResult,
} from '@cratis/arc/queries';
import type { ParameterDescriptor } from '@cratis/arc/reflection';

export interface FakeSuspenseObservableQueryResult {
    id: string;
    name: string;
}

export type SubscribeCallback = OnNextResult<
    QueryResult<FakeSuspenseObservableQueryResult[]>
>;

export class FakeSuspenseObservableQuery extends ObservableQueryFor<
    FakeSuspenseObservableQueryResult[]
> {
    readonly route = '/api/fake-suspense-observable-query';
    readonly parameterDescriptors: ParameterDescriptor[] = [];

    get requiredRequestParameters(): string[] {
        return [];
    }

    defaultValue: FakeSuspenseObservableQueryResult[] = [];

    constructor() {
        super(Object, true);
    }

    static subscribeCallbacks: SubscribeCallback[] = [];
    static subscriptionReturned: ObservableQuerySubscription<
        FakeSuspenseObservableQueryResult[]
    >;
    static unsubscribeCallCount = 0;

    subscribe(
        callback: SubscribeCallback,
    ): ObservableQuerySubscription<FakeSuspenseObservableQueryResult[]> {
        FakeSuspenseObservableQuery.subscribeCallbacks.push(callback);
        let isUnsubscribed = false;
        // SAFETY: The fake implements only the public subscription behavior exercised by these specs.
        FakeSuspenseObservableQuery.subscriptionReturned = {
            unsubscribe: () => {
                if (!isUnsubscribed) {
                    isUnsubscribed = true;
                    FakeSuspenseObservableQuery.unsubscribeCallCount++;
                }
            },
        } as unknown as ObservableQuerySubscription<FakeSuspenseObservableQueryResult[]>;
        return FakeSuspenseObservableQuery.subscriptionReturned;
    }

    static reset() {
        FakeSuspenseObservableQuery.subscribeCallbacks = [];
        FakeSuspenseObservableQuery.unsubscribeCallCount = 0;
    }
}
