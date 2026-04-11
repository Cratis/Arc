// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObservableQueryFor, QueryResult, ObservableQuerySubscription, OnNextResult } from '@cratis/arc/queries';
import { ParameterDescriptor } from '@cratis/arc/reflection';

export interface FakeSingleObservableQueryResult {
    id: string;
    name: string;
}

export type SingleSubscribeCallback = OnNextResult<QueryResult<FakeSingleObservableQueryResult>>;

export class FakeSingleObservableQuery extends ObservableQueryFor<FakeSingleObservableQueryResult> {
    readonly route = '/api/fake-single-observable-query';
    readonly parameterDescriptors: ParameterDescriptor[] = [];

    get requiredRequestParameters(): string[] {
        return [];
    }

    defaultValue: FakeSingleObservableQueryResult = {} as FakeSingleObservableQueryResult;

    constructor() {
        super(Object, false);
    }

    static subscribeCallbacks: SingleSubscribeCallback[] = [];
    static subscriptionReturned: ObservableQuerySubscription<FakeSingleObservableQueryResult>;

    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    subscribe(callback: SingleSubscribeCallback, args?: object): ObservableQuerySubscription<FakeSingleObservableQueryResult> {
        FakeSingleObservableQuery.subscribeCallbacks.push(callback);
        FakeSingleObservableQuery.subscriptionReturned = {
            unsubscribe: () => {}
        } as unknown as ObservableQuerySubscription<FakeSingleObservableQueryResult>;
        return FakeSingleObservableQuery.subscriptionReturned;
    }

    static reset() {
        FakeSingleObservableQuery.subscribeCallbacks = [];
    }
}
