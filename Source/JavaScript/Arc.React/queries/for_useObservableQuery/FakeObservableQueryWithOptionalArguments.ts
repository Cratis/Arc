// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObservableQueryFor, QueryResult, ObservableQuerySubscription, OnNextResult } from '@cratis/arc/queries';
import { ParameterDescriptor } from '@cratis/arc/reflection';

export interface FakeObservableQueryWithOptionalArgumentsResult {
    id: string;
    name: string;
}

export interface FakeObservableQueryWithOptionalArgumentsArguments {
    topicId?: string;
}

export type SubscribeCallback = OnNextResult<QueryResult<FakeObservableQueryWithOptionalArgumentsResult[]>>;

export class FakeObservableQueryWithOptionalArguments extends ObservableQueryFor<FakeObservableQueryWithOptionalArgumentsResult[]> {
    readonly route = '/api/fake-observable-query-with-optional-arguments';
    readonly parameterDescriptors: ParameterDescriptor[] = [];

    static subscribedArgs: (object | undefined)[] = [];

    get requiredRequestParameters(): string[] {
        return [];
    }

    defaultValue: FakeObservableQueryWithOptionalArgumentsResult[] = [];

    constructor() {
        super(Object, true);
    }

    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    subscribe(callback: SubscribeCallback, args?: object): ObservableQuerySubscription<FakeObservableQueryWithOptionalArgumentsResult[]> {
        FakeObservableQueryWithOptionalArguments.subscribedArgs.push(args);
        return {
            unsubscribe: () => { }
        } as ObservableQuerySubscription<FakeObservableQueryWithOptionalArgumentsResult[]>;
    }

    static reset() {
        FakeObservableQueryWithOptionalArguments.subscribedArgs = [];
    }
}
