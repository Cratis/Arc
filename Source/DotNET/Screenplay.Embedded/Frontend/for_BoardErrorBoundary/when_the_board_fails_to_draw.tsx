// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { BoardErrorBoundary } from '../BoardErrorBoundary';

let failing = true;
const Board = () => {
    if (failing) throw new Error('Cannot draw this presentation');
    return <div data-testid='board' />;
};

describe('when the board fails to draw', () => {
    beforeEach(() => {
        failing = true;
        vi.spyOn(console, 'error').mockImplementation(() => undefined);
    });
    afterEach(() => {
        cleanup();
        vi.restoreAllMocks();
    });

    it('says what failed instead of emptying the viewer', () => {
        render(<BoardErrorBoundary resetWhenChanged='full' onReset={() => undefined}><Board /></BoardErrorBoundary>);

        expect(screen.getByRole('alert').textContent).toContain('The board failed to draw: Cannot draw this presentation');
    });

    it('tries the board again when another view option is chosen', () => {
        const { rerender } = render(<BoardErrorBoundary resetWhenChanged='full' onReset={() => undefined}><Board /></BoardErrorBoundary>);
        failing = false;

        rerender(<BoardErrorBoundary resetWhenChanged='overview' onReset={() => undefined}><Board /></BoardErrorBoundary>);

        expect(screen.getByTestId('board')).toBeTruthy();
    });

    it('resets to the default view when asked', () => {
        const onReset = vi.fn(() => { failing = false; });
        render(<BoardErrorBoundary resetWhenChanged='full' onReset={onReset}><Board /></BoardErrorBoundary>);

        fireEvent.click(screen.getByRole('button', { name: 'Show the default view' }));

        expect(onReset).toHaveBeenCalledOnce();
        expect(screen.getByTestId('board')).toBeTruthy();
    });
});
