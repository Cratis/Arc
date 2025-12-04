// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObservableQueryFor, QueryResult, ObservableQuerySubscription, OnNextResult } from '@cratis/arc/queries';
import { ParameterDescriptor } from '@cratis/arc/reflection';

export interface FakeObservableQueryResult {
    id: string;
    name: string;
}

export type SubscribeCallback = OnNextResult<QueryResult<FakeObservableQueryResult[]>>;

export class FakeObservableQuery extends ObservableQueryFor<FakeObservableQueryResult[]> {
    readonly route = '/api/fake-observable-query';
    readonly parameterDescriptors: ParameterDescriptor[] = [];

    get requiredRequestParameters(): string[] {
        return [];
    }

    defaultValue: FakeObservableQueryResult[] = [];

    constructor() {
        super(Object, true);
    }

    static subscribeCallbacks: SubscribeCallback[] = [];
    static subscriptionReturned: ObservableQuerySubscription<FakeObservableQueryResult[]>;

    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    subscribe(callback: SubscribeCallback, args?: object): ObservableQuerySubscription<FakeObservableQueryResult[]> {
        FakeObservableQuery.subscribeCallbacks.push(callback);
        FakeObservableQuery.subscriptionReturned = {
            unsubscribe: () => {}
        } as unknown as ObservableQuerySubscription<FakeObservableQueryResult[]>;
        return FakeObservableQuery.subscriptionReturned;
    }

    static reset() {
        FakeObservableQuery.subscribeCallbacks = [];
    }
}
