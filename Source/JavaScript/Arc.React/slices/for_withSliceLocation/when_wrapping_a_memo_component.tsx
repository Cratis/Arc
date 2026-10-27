// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { memo } from 'react';
import { render } from '@testing-library/react';
import { SliceLocation } from '@cratis/arc/reflection';
import { withSliceLocation } from '../withSliceLocation.js';
import { useSliceContext } from '../useSliceContext.js';

const Summary = withSliceLocation(memo(function Summary() {
    return <span data-testid="summary">{useSliceContext().toString()}</span>;
}), new SliceLocation('Billing', 'Invoices', 'Listing'));

describe('when wrapping a memo component', () => {
    let text: string;

    beforeEach(() => {
        const result = render(<Summary />);
        text = result.getByTestId('summary').textContent!;
        result.unmount();
    });

    it('should stay a memo component', () => ((Summary as unknown as { $$typeof: symbol }).$$typeof).should.equal(Symbol.for('react.memo')));
    it('should keep the name of the wrapped component', () => ((Summary as unknown as { type: { name: string } }).type.name).should.equal('Summary'));
    it('should give the component the folder location', () => text.should.equal('Billing/Invoices/Listing'));
});
