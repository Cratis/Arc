// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { resolveSliceLocation } from '../resolveSliceLocation.js';

describe('when resolving a file under no root', () => {
    it('should give no location without segments to skip', () =>
        (resolveSliceLocation('/app/Shared/Billing/Invoices/Pay/PayDialog.tsx', { projectRoot: '/app', roots: ['Features'] }) === undefined).should.be.true);
    it('should skip the leading segments when segments to skip is set', () =>
        resolveSliceLocation('/app/Shared/Billing/Invoices/Pay/PayDialog.tsx', { projectRoot: '/app', roots: ['Features'], segmentsToSkip: 1 })!.should.deep.equal({ module: 'Billing', feature: 'Invoices', slice: 'Pay' }));
    it('should give no location for a file outside the project root', () =>
        (resolveSliceLocation('/elsewhere/Billing/Invoices/Pay/PayDialog.tsx', { projectRoot: '/app', segmentsToSkip: 0 }) === undefined).should.be.true);
    it('should not treat a sibling folder sharing a prefix as under the root', () =>
        (resolveSliceLocation('/app/FeaturesOld/Billing/Invoices/Pay/PayDialog.tsx', { projectRoot: '/app', roots: ['Features'] }) === undefined).should.be.true);
});
