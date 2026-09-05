// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObservableQueryFor, QueryResult, ObservableQuerySubscription, OnNextResult } from '@cratis/arc/queries';
import { ParameterDescriptor } from '@cratis/arc/reflection';
import { Constructor } from '@cratis/fundamentals';

export type SubscribeCallback = OnNextResult<QueryResult<string[]>>;

/**
 * A fake for the shape a backend `ISubject<IEnumerable<string>>` generates: an observable query
 * over a primitive collection, whose model type is the `String` wrapper.
 */
export class FakeStringObservableQuery extends ObservableQueryFor<string[]> {
    readonly route = '/api/fake-string-observable-query';
    readonly parameterDescriptors: ParameterDescriptor[] = [];

    get requiredRequestParameters(): string[] {
        return [];
    }

    defaultValue: string[] = [];

    constructor() {
        super(String as Constructor, true);
    }

    static subscribeCallbacks: SubscribeCallback[] = [];
    static subscriptionReturned: ObservableQuerySubscription<string[]>;

    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    subscribe(callback: SubscribeCallback, args?: object): ObservableQuerySubscription<string[]> {
        FakeStringObservableQuery.subscribeCallbacks.push(callback);
        FakeStringObservableQuery.subscriptionReturned = {
            unsubscribe: () => {}
        } as unknown as ObservableQuerySubscription<string[]>;
        return FakeStringObservableQuery.subscriptionReturned;
    }

    static reset() {
        FakeStringObservableQuery.subscribeCallbacks = [];
    }
}
