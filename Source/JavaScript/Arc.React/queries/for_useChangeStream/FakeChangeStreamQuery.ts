// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IChangeStreamFor, ObservableQueryFor, QueryResult, ObservableQuerySubscription, OnNextResult } from '@cratis/arc/queries';
import { ParameterDescriptor } from '@cratis/arc/reflection';

export interface FakeItem {
    id: string;
    name: string;
}

export type SubscribeCallback = OnNextResult<QueryResult<FakeItem[]>>;

export class FakeChangeStreamQuery extends ObservableQueryFor<FakeItem[]> implements IChangeStreamFor<FakeItem> {
    readonly route = '/api/fake-change-stream';
    readonly parameterDescriptors: ParameterDescriptor[] = [];

    get requiredRequestParameters(): string[] {
        return [];
    }

    defaultValue: FakeItem[] = [];

    constructor() {
        super(Object, true);
    }

    static subscribeCallbacks: SubscribeCallback[] = [];
    static subscriptionReturned: ObservableQuerySubscription<FakeItem[]>;

    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    subscribe(callback: SubscribeCallback, args?: object): ObservableQuerySubscription<FakeItem[]> {
        FakeChangeStreamQuery.subscribeCallbacks.push(callback);
        FakeChangeStreamQuery.subscriptionReturned = {
            unsubscribe: () => {}
        } as unknown as ObservableQuerySubscription<FakeItem[]>;
        return FakeChangeStreamQuery.subscriptionReturned;
    }

    static reset() {
        FakeChangeStreamQuery.subscribeCallbacks = [];
    }
}
