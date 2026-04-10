// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryFor } from '@cratis/arc/queries';
import { ParameterDescriptor } from '@cratis/arc/reflection';

export interface FakeSingleQueryResult {
    id: string;
    name: string;
}

export class FakeSingleQuery extends QueryFor<FakeSingleQueryResult> {
    readonly route = '/api/fake-single-query';
    readonly parameterDescriptors: ParameterDescriptor[] = [];

    get requiredRequestParameters(): string[] {
        return [];
    }

    defaultValue: FakeSingleQueryResult = {} as FakeSingleQueryResult;

    constructor() {
        super(Object, false);
    }
}
