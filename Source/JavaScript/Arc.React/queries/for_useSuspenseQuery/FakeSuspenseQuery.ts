// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryFor } from '@cratis/arc/queries';
import { ParameterDescriptor } from '@cratis/arc/reflection';

export interface FakeSuspenseQueryResult {
    id: string;
    name: string;
}

export class FakeSuspenseQuery extends QueryFor<FakeSuspenseQueryResult[]> {
    readonly route = '/api/fake-suspense-query';
    readonly parameterDescriptors: ParameterDescriptor[] = [];

    get requiredRequestParameters(): string[] {
        return [];
    }

    defaultValue: FakeSuspenseQueryResult[] = [];

    constructor() {
        super(Object, true);
    }
}
