// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryFor } from '@cratis/arc/queries';
import { ParameterDescriptor, SliceLocation } from '@cratis/arc/reflection';

export class AllInvoices extends QueryFor<string[]> {
    static readonly sliceLocation: SliceLocation = new SliceLocation('Billing', 'Invoices', 'Listing');
    readonly route = '/api/billing/invoices';
    readonly parameterDescriptors: ParameterDescriptor[] = [];
    defaultValue: string[] = [];

    get requiredRequestParameters(): string[] {
        return [];
    }

    constructor() {
        super(String, true);
    }
}
