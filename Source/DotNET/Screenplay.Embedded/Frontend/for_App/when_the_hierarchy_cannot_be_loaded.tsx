// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { failure, json, stubFetch } from '../Specs/given/api';
import { App } from '../App';

vi.mock('@cratis/event-models', () => import('../Specs/given/eventModelsStub'));
vi.mock('@cratis/scene/Prototypes', () => import('../Specs/given/sceneStub'));
vi.mock('@cratis/components/Toolbar', () => import('../Specs/given/toolbarStub'));

describe('when the hierarchy cannot be loaded', () => {
    afterEach(() => vi.unstubAllGlobals());

    it('reports the failure instead of an empty explorer', async () => {
        stubFetch(() => failure(500, 'Internal Server Error'));

        render(<App />);

        const alert = await screen.findByRole('alert');

        expect(alert.textContent).toContain('Could not load the event model documents');
        expect(alert.textContent).toContain('500');
    });

    it('reports that there is nothing to explore when no projects are embedded', async () => {
        stubFetch(() => json([]));

        render(<App />);

        expect((await screen.findByRole('status')).textContent)
            .toContain('No projects with embedded event models were found.');
    });
});
