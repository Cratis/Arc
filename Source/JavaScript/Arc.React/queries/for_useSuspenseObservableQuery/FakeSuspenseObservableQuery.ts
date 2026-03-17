// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObservableQueryFor, QueryResult, ObservableQuerySubscription, OnNextResult } from '@cratis/arc/queries';
import { ParameterDescriptor } from '@cratis/arc/reflection';

export interface FakeSuspenseObservableQueryResult {
    id: string;
    name: string;
}

export type SubscribeCallback = OnNextResult<QueryResult<FakeSuspenseObservableQueryResult[]>>;

export class FakeSuspenseObservableQuery extends ObservableQueryFor<FakeSuspenseObservableQueryResult[]> {
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
    static subscriptionReturned: ObservableQuerySubscription<FakeSuspenseObservableQueryResult[]>;

    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    subscribe(callback: SubscribeCallback, args?: object): ObservableQuerySubscription<FakeSuspenseObservableQueryResult[]> {
        FakeSuspenseObservableQuery.subscribeCallbacks.push(callback);
        FakeSuspenseObservableQuery.subscriptionReturned = {
            unsubscribe: () => {}
        } as unknown as ObservableQuerySubscription<FakeSuspenseObservableQueryResult[]>;
        return FakeSuspenseObservableQuery.subscriptionReturned;
    }

    static reset() {
        FakeSuspenseObservableQuery.subscribeCallbacks = [];
    }
}
