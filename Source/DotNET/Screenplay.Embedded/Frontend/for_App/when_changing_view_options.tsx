// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { respondWithFixtures } from '../Specs/given/api';
import { presentationStorageKey } from '../presentation';
import { App } from '../App';

vi.mock('@cratis/event-models', () => import('../Specs/given/eventModelsStub'));
vi.mock('@cratis/scene/Prototypes', () => import('../Specs/given/sceneStub'));
vi.mock('@cratis/components/Toolbar', () => import('../Specs/given/toolbarStub'));

const openViewMenu = () => fireEvent.click(screen.getByRole('button', { name: 'View' }));
const choose = (option: string) => fireEvent.click(screen.getByRole('menuitemradio', { name: option }));
const checked = (option: string) => screen.getByRole('menuitemradio', { name: option }).getAttribute('aria-checked');

describe('when changing view options', () => {
    afterEach(() => vi.unstubAllGlobals());

    it('offers the same choices Cratis Studio does, starting from the defaults', async () => {
        respondWithFixtures();

        render(<App />);
        await screen.findByTestId('board');
        openViewMenu();

        expect(screen.getAllByRole('menuitemradio').map(item => item.textContent))
            .toEqual(['Full', 'Overview', 'Properties', 'Arrows', 'Lines']);
        expect(checked('Full')).toBe('true');
        expect(checked('Overview')).toBe('false');
        expect(checked('Properties')).toBe('false');
        expect(checked('Arrows')).toBe('true');
        expect(checked('Lines')).toBe('false');
    });

    it('shows every choice the person makes', async () => {
        respondWithFixtures();

        render(<App />);
        await screen.findByTestId('board');
        openViewMenu();

        choose('Overview');
        choose('Properties');
        choose('Lines');

        expect(checked('Overview')).toBe('true');
        expect(checked('Full')).toBe('false');
        expect(checked('Properties')).toBe('true');
        expect(checked('Lines')).toBe('true');
        expect(checked('Arrows')).toBe('false');
    });

    it('remembers them for the next visit', async () => {
        respondWithFixtures();

        render(<App />);
        await screen.findByTestId('board');
        openViewMenu();
        choose('Overview');
        choose('Properties');

        expect(JSON.parse(window.localStorage.getItem(presentationStorageKey)!))
            .toEqual({ detailLevel: 'overview', showProperties: true, visualizationMode: 'simplified' });

        cleanup();
        render(<App />);
        await screen.findByTestId('board');
        openViewMenu();

        expect(checked('Overview')).toBe('true');
        expect(checked('Properties')).toBe('true');
        expect(checked('Arrows')).toBe('true');
    });

    it('starts from the defaults when what was remembered cannot be used', async () => {
        respondWithFixtures();
        window.localStorage.setItem(presentationStorageKey, '{"detailLevel":"microscopic"}');

        render(<App />);
        await screen.findByTestId('board');
        openViewMenu();

        expect(checked('Full')).toBe('true');
        expect(checked('Arrows')).toBe('true');
    });

    it('says so when a choice could not be remembered', async () => {
        respondWithFixtures();
        const setItem = vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => { throw new Error('quota exceeded'); });

        try {
            render(<App />);
            await screen.findByTestId('board');
            openViewMenu();
            choose('Overview');

            const failure = await screen.findByRole('alert');
            expect(failure.textContent).toContain('could not be remembered');
            expect(failure.textContent).toContain('quota exceeded');
            expect(checked('Overview')).toBe('true');
        } finally {
            setItem.mockRestore();
        }
    });
});
