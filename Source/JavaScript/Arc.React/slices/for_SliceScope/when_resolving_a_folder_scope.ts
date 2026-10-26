// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '@cratis/arc/reflection';
import { SliceScope, SliceScopeKind } from '../SliceScope.js';

describe('when resolving a folder scope', () => {
    const declared = new SliceLocation('Billing', 'Invoices', '');
    const folderScope = (location: SliceLocation = declared) => new SliceScope(undefined, location, SliceScopeKind.folder);

    it('should resolve to the folder location without registrations', () => folderScope().resolve().should.equal(declared));

    it('should be refined by a registration below the folder', () => {
        const scope = folderScope();
        const below = new SliceLocation('Billing', 'Invoices', 'Pay');
        scope.register(below);
        scope.resolve().equals(below).should.be.true;
    });

    it('should be refined by a registration below a module folder', () => {
        const scope = folderScope(new SliceLocation('Billing', '', ''));
        const below = new SliceLocation('Billing', 'Invoices', 'Pay');
        scope.register(below);
        scope.resolve().equals(below).should.be.true;
    });

    it('should not be refined by a registration outside the folder', () => {
        const scope = folderScope();
        scope.register(new SliceLocation('Sales', 'Orders', 'Place'));
        scope.resolve().should.equal(declared);
    });

    it('should not be refined by a registration in a folder that only shares a prefix', () => {
        const scope = folderScope();
        scope.register(new SliceLocation('Billing', 'InvoicesArchive', 'Restore'));
        scope.resolve().should.equal(declared);
    });

    it('should not be refined by a registration in a nested feature', () => {
        const scope = folderScope();
        scope.register(new SliceLocation('Billing', 'Invoices.Recurring', 'Pay'));
        scope.resolve().should.equal(declared);
    });

    it('should not be refined by a registration whose parts are joined into the same path differently', () => {
        const scope = folderScope(new SliceLocation('Billing', 'Invoices', 'Pay'));
        scope.register(new SliceLocation('Billing', 'Invoices', 'Pay/Now'));
        scope.resolve().should.equal(scope.declared);
    });
});
