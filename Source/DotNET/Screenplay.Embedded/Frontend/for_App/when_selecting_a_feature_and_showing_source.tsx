// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { respondWithFixtures } from '../Specs/given/api';
import { App } from '../App';

vi.mock('@cratis/event-models', () => import('../Specs/given/eventModelsStub'));
vi.mock('@cratis/scene/Prototypes', () => import('../Specs/given/sceneStub'));
vi.mock('@cratis/components/Toolbar', () => import('../Specs/given/toolbarStub'));
vi.mock('../ScreenplayEditor', () => import('../Specs/given/screenplayEditorStub'));

describe('when selecting a feature and showing its source', () => {
    afterEach(() => vi.unstubAllGlobals());

    it('asks for the selected document and shows the Screenplay source', async () => {
        const requests = respondWithFixtures('feature Checkout {}');

        render(<App />);

        fireEvent.click(await screen.findByRole('treeitem', { name: /Checkout Feature/ }));
        fireEvent.click(screen.getByRole('tab', { name: 'Source' }));

        const source = await screen.findByLabelText('Screenplay source');
        const editor = await screen.findByTestId('screenplay-editor');

        expect(source.contains(editor)).toBe(true);
        expect(editor.textContent).toBe('feature Checkout {}');
        expect(screen.getByRole('tab', { name: 'Source' }).getAttribute('aria-selected')).toBe('true');
        expect(requests.at(-1)!.url).toBe(
            'http://localhost/.cratis/event-model/documents/Acme.Orders/Acme.Orders.Ordering.Checkout/source');
    });
});
