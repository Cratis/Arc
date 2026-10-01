// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** A measured world-space rectangle, as the canvas reports it for one drawn item. */
export interface ContentBounds {
    x: number;
    y: number;
    width: number;
    height: number;
}

/** The part of the page the board is drawn in. */
export interface ViewportSize {
    width: number;
    height: number;
}

/** Where the camera looks and how far out it is: the world point at the centre, and the zoom factor. */
export interface Camera {
    x: number;
    y: number;
    zoom: number;
}

/** What the fit is allowed to do: how much air to leave around the model and how far the zoom may go. */
export interface FitOptions {
    /** Share of the viewport left empty on each side. Defaults to `0.06`. */
    padding?: number;
    /** Lowest zoom the fit will choose. Defaults to `0.1`. */
    minZoom?: number;
    /** Highest zoom the fit will choose - a small model is centred, never magnified past this. Defaults to `1`. */
    maxZoom?: number;
}

/** The fit this module uses when the caller says nothing else. */
export const defaultFitOptions: Required<FitOptions> = { padding: 0.06, minZoom: 0.1, maxZoom: 1 };

const isMeasured = (bounds: ContentBounds) =>
    [bounds.x, bounds.y, bounds.width, bounds.height].every(Number.isFinite) && bounds.width > 0 && bounds.height > 0;

const round = (value: number, decimals: number) => {
    const factor = 10 ** decimals;
    return Math.round(value * factor) / factor;
};

/**
 * The union of every measured item, or `undefined` when nothing has been measured yet. Items the canvas
 * has registered but not measured (zero or non-finite size) are left out: they would drag the union to
 * the origin and the model would be framed around empty space.
 */
export const contentBoundsOf = (items: readonly ContentBounds[]): ContentBounds | undefined => {
    const measured = items.filter(isMeasured);
    if (measured.length === 0) return undefined;

    const left = Math.min(...measured.map(item => item.x));
    const top = Math.min(...measured.map(item => item.y));
    const right = Math.max(...measured.map(item => item.x + item.width));
    const bottom = Math.max(...measured.map(item => item.y + item.height));
    return { x: left, y: top, width: right - left, height: bottom - top };
};

/**
 * The camera that shows the whole model centred in the viewport, or `undefined` when there is nothing
 * to frame yet - no measured item, or a viewport with no size. The result is rounded so that fitting
 * the same layout twice yields exactly the same camera, which is what lets the caller tell a settled
 * layout from a changing one.
 */
export const fitToContent = (
    viewport: ViewportSize,
    items: readonly ContentBounds[],
    options: FitOptions = {}): Camera | undefined => {
    const { padding, minZoom, maxZoom } = { ...defaultFitOptions, ...options };
    if (!(viewport.width > 0) || !(viewport.height > 0)) return undefined;

    const content = contentBoundsOf(items);
    if (!content) return undefined;

    const available = {
        width: viewport.width * Math.max(0, 1 - padding * 2),
        height: viewport.height * Math.max(0, 1 - padding * 2)
    };
    const fitted = Math.min(available.width / content.width, available.height / content.height);
    const zoom = Math.min(maxZoom, Math.max(minZoom, fitted));

    return {
        x: round(content.x + content.width / 2, 2),
        y: round(content.y + content.height / 2, 2),
        zoom: round(zoom, 4)
    };
};

/** Whether two cameras frame the model the same way. */
export const isSameCamera = (left: Camera | undefined, right: Camera | undefined) =>
    left !== undefined && right !== undefined && left.x === right.x && left.y === right.y && left.zoom === right.zoom;
