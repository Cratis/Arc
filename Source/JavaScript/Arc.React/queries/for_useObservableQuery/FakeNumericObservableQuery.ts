// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObservableQueryFor, QueryResult, ObservableQuerySubscription, OnNextResult } from '@cratis/arc/queries';
import { ParameterDescriptor } from '@cratis/arc/reflection';

export interface FakeNumericObservableQueryResult {
    id: number;
    name: string;
}

export type SubscribeCallback = OnNextResult<QueryResult<FakeNumericObservableQueryResult[]>>;

export class FakeNumericObservableQuery extends ObservableQueryFor<FakeNumericObservableQueryResult[]> {
    readonly route = '/api/fake-numeric-observable-query';
    readonly parameterDescriptors: ParameterDescriptor[] = [];

    get requiredRequestParameters(): string[] {
        return [];
    }

    defaultValue: FakeNumericObservableQueryResult[] = [];

    constructor() {
        super(Object, true);
    }

    static subscribeCallbacks: SubscribeCallback[] = [];
    static subscriptionReturned: ObservableQuerySubscription<FakeNumericObservableQueryResult[]>;

    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    subscribe(callback: SubscribeCallback, args?: object): ObservableQuerySubscription<FakeNumericObservableQueryResult[]> {
        FakeNumericObservableQuery.subscribeCallbacks.push(callback);
        FakeNumericObservableQuery.subscriptionReturned = {
            unsubscribe: () => {}
        } as unknown as ObservableQuerySubscription<FakeNumericObservableQueryResult[]>;
        return FakeNumericObservableQuery.subscriptionReturned;
    }

    static reset() {
        FakeNumericObservableQuery.subscribeCallbacks = [];
    }
}