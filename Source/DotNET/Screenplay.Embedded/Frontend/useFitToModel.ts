// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useEffect, useRef } from 'react';
import { fitToContent, isSameCamera, type Camera, type FitOptions } from './fitToContent';

/** The part of the canvas handle the fit needs: where the viewport is, what is drawn, and how to move the camera. */
export interface FitHandle {
    getContainerRect(): DOMRect | null;
    getItemBounds(): { x: number; y: number; width: number; height: number }[];
    smoothPanZoomToWorld(worldX: number, worldY: number, targetZoom?: number, durationMs?: number): void;
}

/** How long the viewer takes to settle on the model when it opens one. */
export const fitDurationMs = 400;

/**
 * Frames the whole model when a document is opened.
 *
 * The board lays out and measures itself over several frames after the canvas hands over its handle,
 * so there is no single moment to fit at and nothing worth waiting a fixed time for. Instead the
 * viewer watches the board's own DOM - items appearing, moving, being resized - and refits on every
 * change until the fit stops moving: a settled layout produces the very same camera twice, which ends
 * it without a timer or a poll. The first thing the person does to the board - a drag, a wheel, a key -
 * ends it too, and from then on the camera is theirs.
 */
export const useFitToModel = (modelKey: string, options?: FitOptions) => {
    const containerRef = useRef<HTMLDivElement | null>(null);
    const handleRef = useRef<FitHandle | null>(null);
    const appliedRef = useRef<Camera | undefined>(undefined);
    const doneRef = useRef(false);
    const optionsRef = useRef(options);
    optionsRef.current = options;

    const fit = useCallback(() => {
        if (doneRef.current) return;
        const handle = handleRef.current;
        if (!handle) return;

        const rect = handle.getContainerRect();
        if (!rect) return;

        const camera = fitToContent({ width: rect.width, height: rect.height }, handle.getItemBounds(), optionsRef.current);
        if (!camera || isSameCamera(camera, appliedRef.current)) return;

        appliedRef.current = camera;
        handle.smoothPanZoomToWorld(camera.x, camera.y, camera.zoom, fitDurationMs);
    }, []);

    const onHandleReady = useCallback((handle: FitHandle) => {
        handleRef.current = handle;
        fit();
    }, [fit]);

    useEffect(() => {
        appliedRef.current = undefined;
        doneRef.current = false;
        const container = containerRef.current;
        if (!container) return;

        const userEvents = ['pointerdown', 'wheel', 'keydown'] as const;
        const mutations = typeof MutationObserver === 'undefined' ? undefined : new MutationObserver(() => fit());
        mutations?.observe(container, { childList: true, subtree: true, attributes: true, attributeFilter: ['style', 'class'] });

        const resizes = typeof ResizeObserver === 'undefined' ? undefined : new ResizeObserver(() => fit());
        resizes?.observe(container);

        const stop = (event: Event) => {
            // The board's toolbar sits on top of it; using the view options is not the person moving the camera.
            if (event.target instanceof Element && event.target.closest('.event-model-board__toolbar')) return;
            doneRef.current = true;
            mutations?.disconnect();
            resizes?.disconnect();
            for (const event of userEvents) container.removeEventListener(event, stop, true);
        };
        for (const event of userEvents) container.addEventListener(event, stop, true);

        fit();
        return () => {
            mutations?.disconnect();
            resizes?.disconnect();
            for (const event of userEvents) container.removeEventListener(event, stop, true);
        };
    }, [modelKey, fit]);

    return { containerRef, onHandleReady };
};
