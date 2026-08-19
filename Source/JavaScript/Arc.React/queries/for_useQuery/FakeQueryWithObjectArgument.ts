// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryFor } from '@cratis/arc/queries';
import { ParameterDescriptor } from '@cratis/arc/reflection';
import { Guid } from '@cratis/fundamentals';

export interface FakeQueryWithObjectArgumentResult {
    id: string;
    name: string;
}

export interface FakeQueryWithObjectArgumentArguments {
    engagementId: Guid;
}

/**
 * A query whose required parameter is an object at runtime - the shape every `Guid`, `DateOnly` and
 * generated concept has, and the one a raw-value dependency array compares by identity.
 */
export class FakeQueryWithObjectArgument extends QueryFor<
    FakeQueryWithObjectArgumentResult[]
> {
    readonly route = '/api/fake-query-with-object-argument';
    readonly parameterDescriptors: ParameterDescriptor[] = [];

    get requiredRequestParameters(): string[] {
        return ['engagementId'];
    }

    defaultValue: FakeQueryWithObjectArgumentResult[] = [];

    constructor() {
        super(Object, true);
    }
}
