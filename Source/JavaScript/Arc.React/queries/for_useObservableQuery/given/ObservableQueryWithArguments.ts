// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObservableQueryFor } from '@cratis/arc/queries';
import { ParameterDescriptor } from '@cratis/arc/reflection';

export class ObservableQueryWithArguments extends ObservableQueryFor<string[]> {
    readonly route = '/observable';
    readonly queryName = 'Specs.ObservableQueryWithArguments';
    readonly parameterDescriptors: ParameterDescriptor[] = [];
    readonly requiredRequestParameters: string[] = [];
    readonly defaultValue: string[] = [];

    constructor() {
        super(String, true);
    }
}
