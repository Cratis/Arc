// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    ObservableQueryFor,
    type ObservableQuerySubscription,
    type OnNextResult,
    QueryFor,
    type QueryResult,
} from '@cratis/arc/queries';
import type { ParameterDescriptor } from '@cratis/arc/reflection';

export interface FakePopulateQueryResult {
    name: string;
    email: string;
}

export const pendingDefaultValue: FakePopulateQueryResult = {
    name: 'Pending default name',
    email: 'pending-default@example.com',
};

export class FakePopulateQuery extends QueryFor<FakePopulateQueryResult> {
    readonly route = '/api/fake-populate-query';
    readonly parameterDescriptors: ParameterDescriptor[] = [];

    get requiredRequestParameters(): string[] {
        return [];
    }

    defaultValue: FakePopulateQueryResult = pendingDefaultValue;

    constructor() {
        super(Object, false);
    }
}

export type PopulateSubscribeCallback = OnNextResult<
    QueryResult<FakePopulateQueryResult>
>;

export class FakeObservablePopulateQuery extends ObservableQueryFor<FakePopulateQueryResult> {
    readonly route = '/api/fake-observable-populate-query';
    readonly parameterDescriptors: ParameterDescriptor[] = [];
    readonly defaultValue: FakePopulateQueryResult = pendingDefaultValue;

    static subscribeCallbacks: PopulateSubscribeCallback[] = [];

    get requiredRequestParameters(): string[] {
        return [];
    }

    constructor() {
        super(Object, false);
    }

    subscribe(
        callback: PopulateSubscribeCallback,
    ): ObservableQuerySubscription<FakePopulateQueryResult> {
        FakeObservablePopulateQuery.subscribeCallbacks.push(callback);
        return {
            unsubscribe: () => {},
        } as ObservableQuerySubscription<FakePopulateQueryResult>;
    }

    static reset(): void {
        FakeObservablePopulateQuery.subscribeCallbacks = [];
    }
}

export class FakeEnumerablePopulateQuery extends QueryFor<FakePopulateQueryResult[]> {
    readonly route = '/api/fake-enumerable-populate-query';
    readonly parameterDescriptors: ParameterDescriptor[] = [];

    get requiredRequestParameters(): string[] {
        return [];
    }

    defaultValue: FakePopulateQueryResult[] = [];

    constructor() {
        super(Object, true);
    }
}
