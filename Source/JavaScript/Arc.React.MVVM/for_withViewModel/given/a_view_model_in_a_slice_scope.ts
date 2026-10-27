// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createElement, ReactNode } from 'react';
import { render } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { Constructor } from '@cratis/fundamentals';
import { ArcContext } from '@cratis/arc.react';
import { Slice, useSliceContext } from '@cratis/arc.react/slices';
import { withViewModel } from '../../withViewModel.js';

const Location = () => createElement('span', { 'data-testid': 'location' }, useSliceContext().toString());

/**
 * Renders a view model component in a slice boundary beside a component reading the slice context, and returns what
 * that component read once everything has settled.
 * @param viewModelType The view model type, registered with the container so its dependencies are resolved through it.
 * @returns The slice location the sibling component reads.
 */
export function locationBesideViewModel<TViewModel extends object>(viewModelType: Constructor<TViewModel>): string {
    const View = withViewModel(viewModelType, () => null);
    const wrap = (children: ReactNode) => createElement(MemoryRouter, null,
        createElement(ArcContext.Provider, { value: { microservice: '', development: false } }, children));

    const result = render(wrap(createElement(Slice, null, createElement(View), createElement(Location))));
    const location = result.getByTestId('location').textContent!;
    result.unmount();
    return location;
}
