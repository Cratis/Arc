// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { transform } from './given.js';

describe('when adding to a module with a component', () => {
    const code = transform('import { useState } from "react";\n\nexport const Pay = () => <div />;')!;

    it('should import withSliceLocation and SliceLocation from the slices package', () =>
        code.should.contain('import { withSliceLocation as __cratisWithSliceLocation, SliceLocation as __CratisSliceLocation } from "@cratis/arc.react/slices";'));
    it('should declare the slice location of the folder', () =>
        code.should.contain('const __cratisSliceLocation = new __CratisSliceLocation("Billing", "Invoices", "Pay");'));
    it('should keep the original imports', () => code.should.contain('import { useState } from "react";'));
    it('should put the declarations after directives', () =>
        transform('\'use client\';\nexport const Pay = () => <div />;')!.startsWith('\'use client\';\nimport { withSliceLocation')!.should.be.true);
});
