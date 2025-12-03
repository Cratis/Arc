// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObservableQueryFor } from '../../ObservableQueryFor';
import { Constructor } from '@cratis/fundamentals';
import { ParameterDescriptor } from '../../../reflection/ParameterDescriptor';

export class TestObservableQuery extends ObservableQueryFor<string, { id: string }> {
    readonly route = '/api/test/{id}';
    readonly defaultValue = '';
    readonly parameterDescriptors: ParameterDescriptor[] = [
        new ParameterDescriptor('id', String as Constructor)
    ];

    get requiredRequestParameters(): string[] {
        return ['id'];
    }

    constructor() {
        super(String as Constructor, false);
    }
}

export class TestEnumerableQuery extends ObservableQueryFor<string[], { category: string }> {
    readonly route = '/api/items/{category}';
    readonly defaultValue: string[] = [];
    readonly parameterDescriptors: ParameterDescriptor[] = [
        new ParameterDescriptor('category', String as Constructor)
    ];

    get requiredRequestParameters(): string[] {
        return ['category'];
    }

    constructor() {
        super(String as Constructor, true);
    }
}

export class TestObservableQueryWithParameterDescriptorValues extends ObservableQueryFor<string, object> {
    readonly route = '/api/search';
    readonly defaultValue = '';
    readonly parameterDescriptors: ParameterDescriptor[] = [
        new ParameterDescriptor('filter', String as Constructor),
        new ParameterDescriptor('limit', Number as Constructor)
    ];

    filter?: string;
    limit?: number;

    get requiredRequestParameters(): string[] {
        return [];
    }

    constructor() {
        super(String as Constructor, false);
    }
}

export interface TestQueryWithRouteAndQueryArgsParameters {
    id: string;
    filter: string;
    limit: number;
}

export class TestObservableQueryWithRouteAndQueryArgs extends ObservableQueryFor<string, TestQueryWithRouteAndQueryArgsParameters> {
    readonly route = '/api/items/{id}';
    readonly defaultValue = '';
    readonly parameterDescriptors: ParameterDescriptor[] = [
        new ParameterDescriptor('id', String as Constructor),
        new ParameterDescriptor('filter', String as Constructor),
        new ParameterDescriptor('limit', Number as Constructor)
    ];

    get requiredRequestParameters(): string[] {
        return ['id'];
    }

    constructor() {
        super(String as Constructor, false);
    }
}