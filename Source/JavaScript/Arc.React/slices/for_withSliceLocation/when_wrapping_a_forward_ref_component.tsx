// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createRef, forwardRef } from 'react';
import { render } from '@testing-library/react';
import { SliceLocation } from '@cratis/arc/reflection';
import { withSliceLocation } from '../withSliceLocation.js';
import { useSliceContext } from '../useSliceContext.js';

const Field = withSliceLocation(forwardRef<HTMLInputElement>(function Field(_props, ref) {
    return <input ref={ref} data-testid="field" defaultValue={useSliceContext().toString()} />;
}), new SliceLocation('Billing', 'Invoices', 'Pay'));

describe('when wrapping a forward ref component', () => {
    const ref = createRef<HTMLInputElement>();
    let value: string;
    let refersToTheElement: boolean;

    beforeEach(() => {
        const result = render(<Field ref={ref} />);
        value = result.getByTestId('field').getAttribute('value')!;
        refersToTheElement = ref.current === result.getByTestId('field');
        result.unmount();
    });

    it('should stay a forward ref component', () => ((Field as unknown as { $$typeof: symbol }).$$typeof).should.equal(Symbol.for('react.forward_ref')));
    it('should keep the name of the component', () => (Field as { displayName?: string }).displayName!.should.equal('Field'));
    it('should forward the ref to the component', () => refersToTheElement.should.be.true);
    it('should give the component the folder location', () => value.should.equal('Billing/Invoices/Pay'));
});
