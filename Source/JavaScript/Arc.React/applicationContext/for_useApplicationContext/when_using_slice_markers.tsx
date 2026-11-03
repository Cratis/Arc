// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render } from '@testing-library/react';
import { ApplicationContext } from '../ApplicationContext.js';
import { Slice } from '../../slices/Slice.js';
import { Host } from './Fixtures.js';

describe('when using slice markers', () => {
    let marker: HTMLElement | null;
    let markedWhenDisabled: boolean;
    let display: string;

    beforeEach(() => {
        const enabled = render(
            <Host store={new ApplicationContext()} sliceMarkers={true}>
                <Slice module="Orders" feature="Editing" slice="ChangeOrder"><button data-testid="target">edit</button></Slice>
            </Host>);
        marker = enabled.getByTestId('target').closest('[data-cratis-module]');
        display = marker!.style.display;
        enabled.unmount();

        const disabled = render(
            <Host store={new ApplicationContext()}>
                <Slice module="Orders" feature="Editing" slice="ChangeOrder"><button data-testid="target">edit</button></Slice>
            </Host>);
        markedWhenDisabled = disabled.container.querySelector('[data-cratis-module]') !== null
            || disabled.container.firstElementChild !== disabled.getByTestId('target');
        disabled.unmount();
    });

    it('should resolve the module from the closest ancestor', () => marker!.getAttribute('data-cratis-module')!.should.equal('Orders'));
    it('should resolve the feature from the closest ancestor', () => marker!.getAttribute('data-cratis-feature')!.should.equal('Editing'));
    it('should resolve the slice from the closest ancestor', () => marker!.getAttribute('data-cratis-slice')!.should.equal('ChangeOrder'));
    it('should not affect layout', () => display.should.equal('contents'));
    it('should render no attributes or wrapper when disabled', () => markedWhenDisabled.should.be.false);
});
