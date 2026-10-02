// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, waitFor } from '@testing-library/react';
import { useEffect } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { useFitToModel, type FitHandle } from '../useFitToModel';

interface Bounds { x: number; y: number; width: number; height: number }

const handleFor = (bounds: () => Bounds[], smoothPanZoomToWorld: FitHandle['smoothPanZoomToWorld']): FitHandle => ({
    getContainerRect: () => ({ width: 1000, height: 500 } as DOMRect),
    getItemBounds: () => bounds(),
    smoothPanZoomToWorld
});

const Board = ({ handle }: { handle: FitHandle }) => {
    const { containerRef, onHandleReady } = useFitToModel('a-document');
    useEffect(() => onHandleReady(handle), [handle, onHandleReady]);
    return (
        <div data-testid='canvas' ref={containerRef}>
            <div className='event-model-board__toolbar'><button type='button' data-testid='view'>View</button></div>
        </div>
    );
};

/** Makes the board's DOM change the way laying out and measuring it does. */
const layoutChanges = (container: HTMLElement) => {
    const item = document.createElement('div');
    item.style.transform = 'translate(1px, 1px)';
    container.appendChild(item);
};

describe('when the board lays itself out', () => {
    it('frames the whole model once the layout is measured, and only then', async () => {
        const camera = vi.fn();
        let bounds: Bounds[] = [];
        const { getByTestId } = render(<Board handle={handleFor(() => bounds, camera)} />);
        const canvas = getByTestId('canvas');

        // Nothing is measured yet, so there is nothing to frame and the camera is left alone.
        layoutChanges(canvas);
        await waitFor(() => expect(canvas.childElementCount).toBe(2));
        expect(camera).not.toHaveBeenCalled();

        bounds = [{ x: 0, y: 0, width: 2000, height: 200 }];
        layoutChanges(canvas);

        await waitFor(() => expect(camera).toHaveBeenCalledTimes(1));
        expect(camera.mock.calls[0].slice(0, 3)).toEqual([1000, 100, 0.44]);
    });

    it('does not move the camera again while the measured layout stays the same', async () => {
        const camera = vi.fn();
        const bounds: Bounds[] = [{ x: 0, y: 0, width: 2000, height: 200 }];
        const { getByTestId } = render(<Board handle={handleFor(() => bounds, camera)} />);
        const canvas = getByTestId('canvas');

        layoutChanges(canvas);
        await waitFor(() => expect(camera).toHaveBeenCalledTimes(1));

        layoutChanges(canvas);
        layoutChanges(canvas);
        await waitFor(() => expect(canvas.childElementCount).toBe(4));

        expect(camera).toHaveBeenCalledTimes(1);
    });

    it('follows the layout while it is still settling', async () => {
        const camera = vi.fn();
        let bounds: Bounds[] = [{ x: 0, y: 0, width: 2000, height: 200 }];
        const { getByTestId } = render(<Board handle={handleFor(() => bounds, camera)} />);
        const canvas = getByTestId('canvas');

        layoutChanges(canvas);
        await waitFor(() => expect(camera).toHaveBeenCalledTimes(1));

        bounds = [{ x: 0, y: 0, width: 4000, height: 200 }];
        layoutChanges(canvas);

        await waitFor(() => expect(camera).toHaveBeenCalledTimes(2));
        expect(camera.mock.calls[1].slice(0, 3)).toEqual([2000, 100, 0.22]);
    });

    it('keeps framing while the person only uses the view options', async () => {
        const camera = vi.fn();
        let bounds: Bounds[] = [{ x: 0, y: 0, width: 2000, height: 200 }];
        const { getByTestId } = render(<Board handle={handleFor(() => bounds, camera)} />);
        const canvas = getByTestId('canvas');

        layoutChanges(canvas);
        await waitFor(() => expect(camera).toHaveBeenCalledTimes(1));

        fireEvent.pointerDown(getByTestId('view'));
        bounds = [{ x: 0, y: 0, width: 4000, height: 200 }];
        layoutChanges(canvas);

        await waitFor(() => expect(camera).toHaveBeenCalledTimes(2));
    });

    it('leaves the camera to the person once they have moved it', async () => {
        const camera = vi.fn();
        let bounds: Bounds[] = [{ x: 0, y: 0, width: 2000, height: 200 }];
        const { getByTestId } = render(<Board handle={handleFor(() => bounds, camera)} />);
        const canvas = getByTestId('canvas');

        layoutChanges(canvas);
        await waitFor(() => expect(camera).toHaveBeenCalledTimes(1));

        fireEvent.wheel(canvas);
        bounds = [{ x: 0, y: 0, width: 8000, height: 200 }];
        layoutChanges(canvas);
        await waitFor(() => expect(canvas.childElementCount).toBe(3));

        expect(camera).toHaveBeenCalledTimes(1);
    });
});
