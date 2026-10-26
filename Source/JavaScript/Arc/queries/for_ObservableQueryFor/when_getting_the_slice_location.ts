// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObservableQueryFor } from '../ObservableQueryFor.js';
import { IQuery } from '../IQuery.js';
import { TestObservableQuery } from './given/TestQueries.js';
import { ParameterDescriptor } from '../../reflection/ParameterDescriptor.js';
import { SliceLocation } from '../../reflection/SliceLocation.js';

class ObserveInvoices extends ObservableQueryFor<string[]> {
    static readonly sliceLocation: SliceLocation = new SliceLocation('Billing', 'Invoices', 'Listing');
    readonly route = '/api/billing/invoices/observe';
    readonly defaultValue: string[] = [];
    readonly parameterDescriptors: ParameterDescriptor[] = [];

    get requiredRequestParameters(): string[] {
        return [];
    }

    constructor() {
        super(String, true);
    }
}

describe('when getting the slice location', () => {
    let declared: SliceLocation | undefined;
    let undeclared: SliceLocation | undefined;

    beforeEach(() => {
        const query: IQuery = new ObserveInvoices();
        declared = SliceLocation.of(query);
        undeclared = SliceLocation.of(new TestObservableQuery());
    });

    it('should return the static slice location of the proxy class', () => declared!.should.equal(ObserveInvoices.sliceLocation));
    it('should return the empty slice location when the class declares none', () => undeclared!.should.equal(SliceLocation.empty));
});
