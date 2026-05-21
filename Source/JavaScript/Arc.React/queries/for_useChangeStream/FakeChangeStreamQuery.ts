// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { IChangeStreamFor, ObservableQueryFor, QueryResult, ObservableQuerySubscription, OnNextResult, QueryInstanceCache } from '@cratis/arc/queries';
import { ParameterDescriptor } from '@cratis/arc/reflection';
import { ArcConfiguration, ArcContext } from '../../ArcContext';
import { QueryInstanceCacheContext } from '../QueryInstanceCacheContext';

export interface FakeItem {
    id: string;
    name: string;
}

export type SubscribeCallback = OnNextResult<QueryResult<FakeItem[]>>;

export abstract class FakeChangeStreamQueryBase extends ObservableQueryFor<FakeItem[]> implements IChangeStreamFor<FakeItem> {
    readonly parameterDescriptors: ParameterDescriptor[] = [];
    abstract readonly route: string;

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
        const queryType = this.constructor as typeof FakeChangeStreamQueryBase;
        queryType.subscribeCallbacks.push(callback);
        queryType.subscriptionReturned = {
            unsubscribe: () => {}
        } as unknown as ObservableQuerySubscription<FakeItem[]>;
        return queryType.subscriptionReturned;
    }

    static reset() {
        this.subscribeCallbacks = [];
        this.subscriptionReturned = undefined as unknown as ObservableQuerySubscription<FakeItem[]>;
    }
}

export const createChangeStreamWrapper = (config: ArcConfiguration) => {
    const queryCache = new QueryInstanceCache();

    return ({ children }: { children: React.ReactNode }) => React.createElement(
        ArcContext.Provider,
        { value: config },
        React.createElement(
            QueryInstanceCacheContext.Provider,
            { value: queryCache },
            children
        )
    );
};
