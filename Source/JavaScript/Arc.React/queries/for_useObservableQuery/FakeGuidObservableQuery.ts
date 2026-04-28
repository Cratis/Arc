// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObservableQueryFor, QueryResult, ObservableQuerySubscription, OnNextResult } from '@cratis/arc/queries';
import { ParameterDescriptor } from '@cratis/arc/reflection';
import { field, Guid } from '@cratis/fundamentals';

export class FakeGuidItem {
    @field(Guid)
    id!: Guid;

    @field(String)
    name!: string;
}

export type SubscribeCallback = OnNextResult<QueryResult<FakeGuidItem[]>>;

export class FakeGuidObservableQuery extends ObservableQueryFor<FakeGuidItem[]> {
    readonly route = '/api/fake-guid-observable-query';
    readonly parameterDescriptors: ParameterDescriptor[] = [];

    get requiredRequestParameters(): string[] {
        return [];
    }

    defaultValue: FakeGuidItem[] = [];

    constructor() {
        super(FakeGuidItem, true);
    }

    static subscribeCallbacks: SubscribeCallback[] = [];
    static subscriptionReturned: ObservableQuerySubscription<FakeGuidItem[]>;

    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    subscribe(callback: SubscribeCallback, args?: object): ObservableQuerySubscription<FakeGuidItem[]> {
        FakeGuidObservableQuery.subscribeCallbacks.push(callback);
        FakeGuidObservableQuery.subscriptionReturned = {
            unsubscribe: () => {}
        } as unknown as ObservableQuerySubscription<FakeGuidItem[]>;
        return FakeGuidObservableQuery.subscriptionReturned;
    }

    static reset() {
        FakeGuidObservableQuery.subscribeCallbacks = [];
    }
}
