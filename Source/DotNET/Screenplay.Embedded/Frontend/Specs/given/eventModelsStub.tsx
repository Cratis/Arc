// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createContext, useContext, useState, type ReactNode } from 'react';
import { Toolbar, ToolbarButton } from './toolbarStub';

// The published board draws on a WebGL canvas, which jsdom has nothing to render with. These specs are
// about what the viewer asks the board for - the document, read-only, how the canvas is set up and whether
// the board offers its view options - so the board stands in for itself and records what it was given.
export const EventModelBoard = ({ document, readOnly, showViewOptions, canvas }: {
    document: { name?: string };
    readOnly?: boolean;
    showViewOptions?: boolean;
    canvas?: { controlsPlacement?: string; disableControlsGlass?: boolean; chrome?: { controlsFollowViewportInsets?: boolean } };
}) => (
    <>
        <div
            data-testid='board'
            data-read-only={String(!!readOnly)}
            data-controls-placement={canvas?.controlsPlacement ?? ''}
            data-controls-glass-disabled={String(!!canvas?.disableControlsGlass)}
            data-controls-follow-insets={String(canvas?.chrome?.controlsFollowViewportInsets ?? true)}>
            {document?.name}
        </div>
        {showViewOptions && <ViewOptions />}
    </>
);

const PassThrough = ({ children }: { children?: ReactNode }) => <>{children}</>;

export const EventModelMessagesProvider = PassThrough;

interface Presentation {
    detailLevel: string;
    visualizationMode: string;
    detailsVisibility: DetailsVisibilityState;
}

const PresentationContext = createContext<{ presentation?: Presentation; onChange?: (presentation: Presentation) => void }>({});

export const EventModelPresentationProvider = ({ presentation, onChange, children }: {
    presentation: Presentation;
    onChange?: (presentation: Presentation) => void;
    children?: ReactNode;
}) => <PresentationContext.Provider value={{ presentation, onChange }}>{children}</PresentationContext.Provider>;
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

// Stands in for the board's own view options: the same menu in the board's toolbar, handing the chosen
// presentation to the provider's onChange as the package does.
const ViewOptions = () => {
    const { presentation, onChange } = useContext(PresentationContext);
    if (!presentation) return null;
    const change = (next: Partial<Presentation>) => onChange?.({ ...presentation, ...next });
    const items: MenuDropdownItem[] = [
        { id: 'detail-level-header', isHeader: true, label: 'Detail level' },
        { id: 'detail-level-full', label: 'Full', checked: presentation.detailLevel === 'full', onClick: () => change({ detailLevel: 'full' }) },
        { id: 'detail-level-overview', label: 'Overview', checked: presentation.detailLevel === 'overview', onClick: () => change({ detailLevel: 'overview' }) },
        { id: 'properties-separator', isSeparator: true },
        { id: 'properties-header', isHeader: true, label: 'Properties' },
        {
            id: 'properties', label: 'Properties', checked: presentation.detailsVisibility.global,
            onClick: () => change({ detailsVisibility: toggleGlobalDetails(presentation.detailsVisibility) })
        },
        { id: 'visualization-separator', isSeparator: true },
        { id: 'visualization-header', isHeader: true, label: 'Visualization' },
        { id: 'visualization-arrows', label: 'Arrows', checked: presentation.visualizationMode === 'simplified', onClick: () => change({ visualizationMode: 'simplified' }) },
        { id: 'visualization-lines', label: 'Lines', checked: presentation.visualizationMode === 'fillLines', onClick: () => change({ visualizationMode: 'fillLines' }) }
    ];
    return (
        <div className='event-model-board__toolbar'>
            <Toolbar orientation='horizontal' aria-label='Board'>
                <MenuDropdown trigger={<ToolbarButton icon='pi pi-eye' title='View' tooltipPosition='bottom' />} items={items} />
            </Toolbar>
        </div>
    );
};
