// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { defaultDetailsVisibilityState, type DetailLevel, type EventModelPresentation, type VisualizationMode } from '@cratis/event-models';

/** Where the viewer keeps the view options the person last chose. */
export const presentationStorageKey = 'cratis.screenplay.viewer.view-options';

/** The view options a first-time visitor gets: the whole model, properties hidden, connections as arrows. */
export const defaultPresentation: EventModelPresentation = {
    detailLevel: 'full',
    visualizationMode: 'simplified',
    detailsVisibility: defaultDetailsVisibilityState
};

/** The shape written to storage - only what the person chose, never the per-slice visibility map. */
export interface StoredViewOptions {
    detailLevel: DetailLevel;
    showProperties: boolean;
    visualizationMode: VisualizationMode;
}

const detailLevels: readonly DetailLevel[] = ['full', 'overview'];
const visualizationModes: readonly VisualizationMode[] = ['simplified', 'fillLines'];

const isOneOf = <T extends string>(allowed: readonly T[], value: unknown): value is T =>
    typeof value === 'string' && (allowed as readonly string[]).includes(value);

/** The stored view options as the viewer presents them. */
export const toPresentation = (stored: StoredViewOptions): EventModelPresentation => ({
    detailLevel: stored.detailLevel,
    visualizationMode: stored.visualizationMode,
    detailsVisibility: { ...defaultDetailsVisibilityState, global: stored.showProperties }
});

/** What the viewer keeps of a presentation. */
export const toStoredViewOptions = (presentation: EventModelPresentation): StoredViewOptions => ({
    detailLevel: presentation.detailLevel,
    showProperties: presentation.detailsVisibility.global,
    visualizationMode: presentation.visualizationMode
});

/**
 * The view options in the given text, with every field the viewer does not recognize replaced by the
 * default for it. A value written by an older - or a tampered with - viewer never reaches the board.
 */
export const parseViewOptions = (raw: string | null | undefined): StoredViewOptions => {
    const fallback = toStoredViewOptions(defaultPresentation);
    if (!raw) return fallback;

    let parsed: unknown;
    try {
        parsed = JSON.parse(raw);
    } catch {
        return fallback;
    }
    if (typeof parsed !== 'object' || parsed === null) return fallback;

    const candidate = parsed as Partial<StoredViewOptions>;
    return {
        detailLevel: isOneOf(detailLevels, candidate.detailLevel) ? candidate.detailLevel : fallback.detailLevel,
        showProperties: typeof candidate.showProperties === 'boolean' ? candidate.showProperties : fallback.showProperties,
        visualizationMode: isOneOf(visualizationModes, candidate.visualizationMode)
            ? candidate.visualizationMode
            : fallback.visualizationMode
    };
};

/**
 * The presentation remembered from the last visit. A storage that cannot be read at all - a browser
 * with storage denied - is the same as never having chosen anything, so the viewer starts from the
 * defaults rather than failing to open.
 */
export const readPresentation = (storage: Storage | undefined): EventModelPresentation => {
    if (!storage) return defaultPresentation;
    try {
        return toPresentation(parseViewOptions(storage.getItem(presentationStorageKey)));
    } catch {
        return defaultPresentation;
    }
};

/**
 * Writes the chosen view options. Throws when they could not be stored - the caller has to tell the
 * person that this choice will not survive a reload rather than quietly pretend it was kept.
 */
export const writePresentation = (storage: Storage | undefined, presentation: EventModelPresentation): void => {
    if (!storage) {
        throw new Error('This browser has no storage for the viewer to remember view options in');
    }
    storage.setItem(presentationStorageKey, JSON.stringify(toStoredViewOptions(presentation)));
};
