// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    ObservableQueryFor,
    QueryResult,
    ObservableQuerySubscription,
    OnNextResult
} from '@cratis/arc/queries';
import { ParameterDescriptor } from '@cratis/arc/reflection';

export interface FakeObservableQueryWithRequiredParametersResult {
    id: string;
    name: string;
}

export interface FakeObservableQueryWithRequiredParametersArguments {
    userId: string;
    category: string;
}

export type SubscribeCallback = OnNextResult<QueryResult<FakeObservableQueryWithRequiredParametersResult[]>>;

export class FakeObservableQueryWithRequiredParameters extends ObservableQueryFor<FakeObservableQueryWithRequiredParametersResult[]> {
    readonly route = '/api/fake-observable-query-with-required-parameters';
    readonly parameterDescriptors: ParameterDescriptor[] = [];

    static subscribeCallCount = 0;

    get requiredRequestParameters(): string[] {
        return ['userId', 'category'];
    }

    defaultValue: FakeObservableQueryWithRequiredParametersResult[] = [];

    constructor() {
        super(Object, true);
    }

    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    subscribe(callback: SubscribeCallback, args?: object): ObservableQuerySubscription<FakeObservableQueryWithRequiredParametersResult[]> {
        FakeObservableQueryWithRequiredParameters.subscribeCallCount++;
        return {
            unsubscribe: () => { }
        } as ObservableQuerySubscription<FakeObservableQueryWithRequiredParametersResult[]>;
    }

    static reset() {
        FakeObservableQueryWithRequiredParameters.subscribeCallCount = 0;
    }
}