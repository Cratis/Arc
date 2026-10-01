// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { contentBoundsOf, fitToContent, isSameCamera } from '../fitToContent';

describe('when framing a model', () => {
    const viewport = { width: 1000, height: 500 };

    it('centres the camera on everything that is drawn', () => {
        const camera = fitToContent(viewport, [
            { x: 0, y: 0, width: 100, height: 100 },
            { x: 300, y: 100, width: 100, height: 100 }
        ]);

        expect(camera).toEqual({ x: 200, y: 100, zoom: 1 });
    });

    it('zooms out far enough to show a model wider than the viewport', () => {
        const camera = fitToContent(viewport, [{ x: 0, y: 0, width: 4000, height: 200 }]);

        // 1000 wide less 6% padding on each side leaves 880 for 4000 world units.
        expect(camera).toEqual({ x: 2000, y: 100, zoom: 0.22 });
    });

    it('zooms out to the taller of the two directions', () => {
        const camera = fitToContent(viewport, [{ x: 0, y: 0, width: 500, height: 2000 }]);

        // 500 high less 6% padding on each side leaves 440 for 2000 world units.
        expect(camera).toEqual({ x: 250, y: 1000, zoom: 0.22 });
    });

    it('never magnifies a small model past its own size', () => {
        const camera = fitToContent(viewport, [{ x: 10, y: 20, width: 10, height: 10 }]);

        expect(camera).toEqual({ x: 15, y: 25, zoom: 1 });
    });

    it('never zooms out below what the canvas allows', () => {
        const camera = fitToContent(viewport, [{ x: 0, y: 0, width: 1000000, height: 1000 }], { minZoom: 0.1 });

        expect(camera!.zoom).toBe(0.1);
    });

    it('has nothing to frame before anything is measured', () => {
        expect(fitToContent(viewport, [])).toBeUndefined();
        expect(fitToContent(viewport, [{ x: 0, y: 0, width: 0, height: 0 }])).toBeUndefined();
    });

    it('has nothing to frame while the viewport has no size', () => {
        expect(fitToContent({ width: 0, height: 0 }, [{ x: 0, y: 0, width: 100, height: 100 }])).toBeUndefined();
    });

    it('leaves unmeasured items out of the model it frames', () => {
        expect(contentBoundsOf([
            { x: 1000, y: 1000, width: 100, height: 100 },
            { x: 0, y: 0, width: 0, height: 0 },
            { x: Number.NaN, y: 0, width: 50, height: 50 }
        ])).toEqual({ x: 1000, y: 1000, width: 100, height: 100 });
    });

    it('frames the same layout the same way twice', () => {
        const items = [{ x: 3, y: 7, width: 333, height: 777 }];

        expect(isSameCamera(fitToContent(viewport, items), fitToContent(viewport, items))).toBe(true);
        expect(isSameCamera(fitToContent(viewport, items), undefined)).toBe(false);
        expect(isSameCamera(undefined, undefined)).toBe(false);
    });
});
