// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { respondWithFixtures } from '../Specs/given/api';
import { App } from '../App';

vi.mock('@cratis/event-models', () => import('../Specs/given/eventModelsStub'));
vi.mock('@cratis/scene/Prototypes', () => import('../Specs/given/sceneStub'));
vi.mock('@cratis/components/Toolbar', () => import('../Specs/given/toolbarStub'));

describe('when opening the explorer', () => {
    afterEach(() => vi.unstubAllGlobals());

    it('shows the hierarchy, opens on the root document and draws it read-only', async () => {
        const requests = respondWithFixtures();

        render(<App />);

        expect(screen.getByRole('status').textContent).toContain('Loading event model documents');

        const board = await screen.findByTestId('board');

        expect(screen.getByRole('treeitem', { name: /Acme\.Orders Assembly/ }).getAttribute('aria-selected')).toBe('true');
        expect(screen.getByRole('treeitem', { name: /Ordering Module/ }).getAttribute('aria-selected')).toBe('false');
        expect(screen.getByRole('treeitem', { name: /Checkout Feature/ })).toBeTruthy();
        expect(board.getAttribute('data-read-only')).toBe('true');
        // The zoom controls sit in the lower right as Cratis Studio has them, on their own frosted pill, and
        // do not step aside for a viewport inset the viewer already applied by placing the canvas.
        expect(board.getAttribute('data-controls-placement')).toBe('bottom-right');
        expect(board.getAttribute('data-controls-glass-disabled')).toBe('true');
        expect(board.getAttribute('data-controls-follow-insets')).toBe('false');
        expect(screen.getByRole('button', { name: 'View' }).closest('.board-view-options')).toBeTruthy();
        const brand = screen.getByRole('link', { name: 'Visit Cratis (opens in a new tab)' });
        expect(brand.getAttribute('href')).toBe('https://cratis.io');
        expect(brand.getAttribute('target')).toBe('_blank');
        expect(brand.getAttribute('rel')).toBe('noopener noreferrer');
        expect(screen.getByRole('img', { name: 'Cratis' }).closest('.sidebar')).toBeTruthy();
        expect(brand.closest('.sidebar')?.firstElementChild).toBe(brand);
        expect(screen.getByRole('list', { name: 'Event model warnings' }).textContent).toContain('Screens are not shown on the canvas');

        await waitFor(() => expect(requests.map(request => request.url)).toEqual([
            'http://localhost/.cratis/event-model/hierarchy',
            'http://localhost/.cratis/event-model/documents/Acme.Orders/Acme.Orders/model'
        ]));
    });
});
