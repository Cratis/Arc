// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Toolbar, ToolbarButton } from '@cratis/components/Toolbar';
import { MenuDropdown, toggleGlobalDetails, type EventModelPresentation, type MenuDropdownItem } from '@cratis/event-models';
import type { PresentationChange } from './usePresentation';

export interface ViewOptionsProps {
    readonly presentation: EventModelPresentation;
    readonly onChange: (change: PresentationChange) => void;
}

/**
 * The view options Cratis Studio offers above the board, in the same place and with the same choices:
 * how much of each slice is drawn, whether properties are shown, and how connections are drawn.
 */
export const ViewOptions = ({ presentation, onChange }: ViewOptionsProps) => {
    const items: MenuDropdownItem[] = [
        { id: 'detail-level-header', isHeader: true, label: 'Detail level' },
        {
            id: 'detail-level-full', label: 'Full', icon: <i className='pi pi-th-large' />, checked: presentation.detailLevel === 'full',
            onClick: () => onChange(current => ({ ...current, detailLevel: 'full' }))
        },
        {
            id: 'detail-level-overview', label: 'Overview', icon: <i className='pi pi-bars' />, checked: presentation.detailLevel === 'overview',
            onClick: () => onChange(current => ({ ...current, detailLevel: 'overview' }))
        },
        { id: 'properties-separator', isSeparator: true },
        { id: 'properties-header', isHeader: true, label: 'Properties' },
        {
            id: 'properties', label: 'Properties', icon: <i className='pi pi-list' />, checked: presentation.detailsVisibility.global,
            onClick: () => onChange(current => ({ ...current, detailsVisibility: toggleGlobalDetails(current.detailsVisibility) }))
        },
        { id: 'visualization-separator', isSeparator: true },
        { id: 'visualization-header', isHeader: true, label: 'Visualization' },
        {
            id: 'visualization-arrows', label: 'Arrows', icon: <i className='pi pi-arrow-right' />, checked: presentation.visualizationMode === 'simplified',
            onClick: () => onChange(current => ({ ...current, visualizationMode: 'simplified' }))
        },
        {
            id: 'visualization-lines', label: 'Lines', icon: <i className='pi pi-share-alt' />, checked: presentation.visualizationMode === 'fillLines',
            onClick: () => onChange(current => ({ ...current, visualizationMode: 'fillLines' }))
        }
    ];

    return (
        <div className='board-view-options' onPointerDown={event => event.stopPropagation()}>
            <Toolbar orientation='horizontal' aria-label='Board'>
                <MenuDropdown
                    trigger={<ToolbarButton icon='pi pi-eye' title='View' tooltipPosition='bottom' />}
                    items={items}
                    align='right' />
            </Toolbar>
        </div>
    );
};
