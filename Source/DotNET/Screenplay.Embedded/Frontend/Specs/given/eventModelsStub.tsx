// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, type ReactNode } from 'react';

// The published board draws on a WebGL canvas, which jsdom has nothing to render with. These specs are
// about what the viewer asks the board for - the document, read-only, and how the canvas is set up - so
// the board stands in for itself and records the canvas options it was given.
export const EventModelBoard = ({ document, readOnly, canvas }: {
    document: { name?: string };
    readOnly?: boolean;
    canvas?: { controlsPlacement?: string; disableControlsGlass?: boolean; chrome?: { controlsFollowViewportInsets?: boolean } };
}) => (
    <div
        data-testid='board'
        data-read-only={String(!!readOnly)}
        data-controls-placement={canvas?.controlsPlacement ?? ''}
        data-controls-glass-disabled={String(!!canvas?.disableControlsGlass)}
        data-controls-follow-insets={String(canvas?.chrome?.controlsFollowViewportInsets ?? true)}>
        {document?.name}
    </div>
);

const PassThrough = ({ children }: { children?: ReactNode }) => <>{children}</>;

export const EventModelMessagesProvider = PassThrough;
export const EventModelPresentationProvider = PassThrough;
export const MenuDropdownOpenProvider = PassThrough;
export const defaultEventModelMessages = {};

export interface DetailsVisibilityState {
    global: boolean;
    modules: Record<string, boolean>;
    features: Record<string, boolean>;
    slices: Record<string, boolean>;
}

export const defaultDetailsVisibilityState: DetailsVisibilityState = { global: false, modules: {}, features: {}, slices: {} };

export const toggleGlobalDetails = (state: DetailsVisibilityState): DetailsVisibilityState => ({ ...state, global: !state.global });

export const defaultEventModelPresentation = {
    detailLevel: 'full',
    visualizationMode: 'simplified',
    detailsVisibility: defaultDetailsVisibilityState
};

export const readEventModelDocument = (json: unknown) => json;

export interface MenuDropdownItem {
    id: string;
    label?: string;
    icon?: ReactNode;
    onClick?: () => void;
    isSeparator?: boolean;
    isHeader?: boolean;
    checked?: boolean;
}

// Stands in for the real dropdown with the same behaviour the specs care about: a trigger that opens a
// menu, and items that report what they are checked as and act when chosen.
export const MenuDropdown = ({ trigger, items }: { trigger: ReactNode; items: MenuDropdownItem[] }) => {
    const [open, setOpen] = useState(false);
    return (
        <div>
            <span onClick={() => setOpen(current => !current)}>{trigger}</span>
            {open && (
                <div role='menu'>
                    {items.map(item => {
                        if (item.isSeparator) return <hr key={item.id} />;
                        if (item.isHeader) return <div key={item.id} role='presentation'>{item.label}</div>;
                        return (
                            <button
                                key={item.id}
                                type='button'
                                role='menuitemradio'
                                aria-checked={!!item.checked}
                                onClick={() => item.onClick?.()}>
                                {item.label}
                            </button>
                        );
                    })}
                </div>
            )}
        </div>
    );
};
