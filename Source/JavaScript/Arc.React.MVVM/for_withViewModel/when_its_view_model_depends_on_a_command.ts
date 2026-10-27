// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// @vitest-environment jsdom

import { container } from 'tsyringe';
import { PayInvoice } from './given/PayInvoice.js';
import { locationBesideViewModel } from './given/a_view_model_in_a_slice_scope.js';

class PayViewModel {
    constructor(readonly payInvoice: PayInvoice) {
    }
}

describe('when its view model depends on a command', () => {
    let location: string;

    beforeEach(() => {
        // Resolve the dependency through the container the way constructor injection does, without decorator metadata.
        container.register(PayViewModel, { useFactory: dependencies => new PayViewModel(dependencies.resolve(PayInvoice)) });
        location = locationBesideViewModel(PayViewModel);
    });

    it('should register the slice location of the command on the nearest scope', () => location.should.equal('Billing/Invoices/Pay'));
});
