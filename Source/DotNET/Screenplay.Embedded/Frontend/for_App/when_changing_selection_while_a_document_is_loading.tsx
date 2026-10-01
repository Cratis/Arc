// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { hierarchy, json, stubFetch, text } from '../Specs/given/api';
import { App } from '../App';

vi.mock('@cratis/event-models', () => import('../Specs/given/eventModelsStub'));
vi.mock('@cratis/scene/Prototypes', () => import('../Specs/given/sceneStub'));
vi.mock('@cratis/components/Toolbar', () => import('../Specs/given/toolbarStub'));
vi.mock('../ScreenplayEditor', () => import('../Specs/given/screenplayEditorStub'));

describe('when changing selection while a document is loading', () => {
    afterEach(() => vi.unstubAllGlobals());

    it('abandons the request in flight so the slower answer cannot win', async () => {
        let releaseFirstSource: (() => void) | undefined;
        const requests = stubFetch(({ url }) => {
            if (url.endsWith('/hierarchy')) return json(hierarchy);
            if (url.includes('/Acme.Orders/source')) {
                return new Promise<Response>(resolve => {
                    releaseFirstSource = () => resolve(text('the stale assembly source'));
                });
            }
            return text('the selected feature source');
        });

        render(<App />);

        fireEvent.click(await screen.findByRole('tab', { name: 'Source' }));
        await waitFor(() => expect(requests.some(request => request.url.includes('/Acme.Orders/source'))).toBe(true));

        fireEvent.click(screen.getByRole('treeitem', { name: /Checkout Feature/ }));

        const stale = requests.find(request => request.url.includes('/Acme.Orders/source'))!;
        expect(stale.signal.aborted).toBe(true);

        releaseFirstSource?.();

        const editor = await screen.findByTestId('screenplay-editor');
        expect(editor.textContent).toBe('the selected feature source');
        expect(screen.queryByText(/stale assembly source/)).toBeNull();
    });
});
