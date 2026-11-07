// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createElement, StrictMode } from 'react';
import { render, act } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { container } from 'tsyringe';
import sinon from 'sinon';
import { ArcContext } from '@cratis/arc.react';
import { withViewModel } from '../../withViewModel.js';

export function renderViewModel(development: boolean, strict = false) {
    let detached = 0;
    let constructed = 0;
    class ViewModel {
        constructor() {
            constructed++;
        }
        detached() {
            detached++;
        }
    }

    const createChild = container.createChildContainer.bind(container);
    const disposals: sinon.SinonSpy[] = [];
    const childStub = sinon.stub(container, 'createChildContainer').callsFake(() => {
        const child = createChild();
        disposals.push(sinon.spy(child, 'dispose'));
        return child;
    });
    const View = withViewModel(ViewModel, () => null);
    const view = createElement(MemoryRouter, null,
        createElement(ArcContext.Provider, { value: { microservice: '', development } }, createElement(View)));
    const rendered = render(strict ? createElement(StrictMode, null, view) : view);
    return {
        rendered,
        get detached() { return detached; },
        get constructed() { return constructed; },
        disposals,
        restore: () => childStub.restore()
    };
}

export async function flushDisposal() {
    await act(async () => {
        await new Promise(resolve => setTimeout(resolve, 0));
    });
}
