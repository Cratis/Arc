// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryFor } from '@cratis/arc/queries';
import { ParameterDescriptor } from '@cratis/arc/reflection';

export interface FakePopulateQueryResult {
    name: string;
    email: string;
}

export class FakePopulateQuery extends QueryFor<FakePopulateQueryResult> {
    readonly route = '/api/fake-populate-query';
    readonly parameterDescriptors: ParameterDescriptor[] = [];

    get requiredRequestParameters(): string[] {
        return [];
    }

    defaultValue: FakePopulateQueryResult = {} as FakePopulateQueryResult;

    constructor() {
        super(Object, false);
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
