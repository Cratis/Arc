// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryFor } from '@cratis/arc/queries';
import { ParameterDescriptor } from '@cratis/arc/reflection';

export interface FakeQueryWithRequiredParametersResult {
    id: string;
    name: string;
}

export interface FakeQueryWithRequiredParametersArguments {
    userId: string;
    category: string;
}

export class FakeQueryWithRequiredParameters extends QueryFor<FakeQueryWithRequiredParametersResult[]> {
    readonly route = '/api/fake-query-with-required-parameters';
    readonly parameterDescriptors: ParameterDescriptor[] = [];

    get requiredRequestParameters(): string[] {
        return ['userId', 'category'];
    }

    defaultValue: FakeQueryWithRequiredParametersResult[] = [];

    constructor() {
        super(Object, true);
    }
}
