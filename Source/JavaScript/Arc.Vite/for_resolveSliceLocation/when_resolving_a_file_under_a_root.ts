// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { resolveSliceLocation } from '../resolveSliceLocation.js';

describe('when resolving a file under a root', () => {
    const mapping = { projectRoot: '/app', roots: ['Features', 'Features/Billing/Legacy'] };

    it('should map module, feature and slice', () =>
        resolveSliceLocation('/app/Features/Billing/Invoices/Pay/PayDialog.tsx', mapping)!.should.deep.equal({ module: 'Billing', feature: 'Invoices', slice: 'Pay' }));
    it('should join the folders between module and slice into the feature', () =>
        resolveSliceLocation('/app/Features/Billing/Invoices/Recurring/Pay/PayDialog.tsx', mapping)!.should.deep.equal({ module: 'Billing', feature: 'Invoices.Recurring', slice: 'Pay' }));
    it('should fill the deepest parts for a shallow folder', () =>
        resolveSliceLocation('/app/Features/Billing/Invoices/Invoices.tsx', mapping)!.should.deep.equal({ module: '', feature: 'Billing', slice: 'Invoices' }));
    it('should use the longest matching root', () =>
        resolveSliceLocation('/app/Features/Billing/Legacy/Archive/Restore/Restore.tsx', mapping)!.should.deep.equal({ module: '', feature: 'Archive', slice: 'Restore' }));
    it('should accept a path relative to the project root', () =>
        resolveSliceLocation('Features/Billing/Invoices/Pay/PayDialog.tsx', mapping)!.should.deep.equal({ module: 'Billing', feature: 'Invoices', slice: 'Pay' }));
    it('should give no location for a file directly in a root', () => (resolveSliceLocation('/app/Features/App.tsx', mapping) === undefined).should.be.true);
});
