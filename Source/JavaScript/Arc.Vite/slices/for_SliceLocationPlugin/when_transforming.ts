// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocationPlugin } from '../SliceLocationPlugin.js';

describe('when transforming', () => {
    const component = 'export const Pay = () => <div />;';
    const plugin = SliceLocationPlugin({ roots: ['Features'], exclude: /[\\/]Shared[\\/]/ });
    plugin.configResolved({ root: '/app' });

    const transform = (id: string) => plugin.transform(component, id);

    it('should add the slice location to a component in a slice folder', () =>
        transform('/app/Features/Billing/Invoices/Pay/Pay.tsx')!.code.should.contain('new __CratisSliceLocation("Billing", "Invoices", "Pay")'));
    it('should ignore a query string on the module id', () =>
        (transform('/app/Features/Billing/Invoices/Pay/Pay.tsx?v=123') !== undefined).should.be.true);
    it('should leave a file outside the roots alone', () => (transform('/app/Components/Pay.tsx') === undefined).should.be.true);
    it('should leave an excluded file alone', () => (transform('/app/Features/Shared/Buttons/Report.tsx') === undefined).should.be.true);
    it('should leave a file that is not tsx or jsx alone', () => (transform('/app/Features/Billing/Invoices/Pay/Pay.ts') === undefined).should.be.true);
    it('should leave a file in node_modules alone', () => (transform('/app/Features/node_modules/x/Billing/Pay.tsx') === undefined).should.be.true);
    it('should leave a virtual module alone', () => (transform('\0/app/Features/Billing/Invoices/Pay/Pay.tsx') === undefined).should.be.true);
});
