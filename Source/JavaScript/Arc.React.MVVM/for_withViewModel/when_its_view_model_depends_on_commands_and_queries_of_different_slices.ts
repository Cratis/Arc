// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// @vitest-environment jsdom

import { container } from 'tsyringe';
import { PayInvoice } from './given/PayInvoice.js';
import { AllInvoices } from './given/AllInvoices.js';
import { locationBesideViewModel } from './given/a_view_model_in_a_slice_scope.js';

class InvoicesViewModel {
    constructor(readonly payInvoice: PayInvoice, readonly allInvoices: AllInvoices) {
    }
}

describe('when its view model depends on commands and queries of different slices', () => {
    let location: string;

    beforeEach(() => {
        container.register(InvoicesViewModel, {
            useFactory: dependencies => new InvoicesViewModel(dependencies.resolve(PayInvoice), dependencies.resolve(AllInvoices))
        });
        location = locationBesideViewModel(InvoicesViewModel);
    });

    it('should register the locations so the scope resolves the parts they share', () => location.should.equal('Billing/Invoices'));
});
