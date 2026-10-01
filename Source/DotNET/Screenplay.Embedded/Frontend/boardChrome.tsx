// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { BoardCanvasChrome } from '@cratis/event-models';

/**
 * The board's zoom controls as Cratis Studio draws them. The viewer already places the canvas beside
 * its sidebar, so the controls must not step aside for a viewport inset a second time.
 */
export const boardChrome: BoardCanvasChrome = {
    controlsLabels: { toggleMinimap: 'Toggle minimap', zoomOut: 'Zoom out', resetZoom: 'Reset zoom', zoomIn: 'Zoom in', help: 'Help' },
    controlsIcons: {
        toggleMinimap: <i className='pi pi-th-large' aria-hidden />,
        zoomOut: <i className='pi pi-minus' aria-hidden />,
        zoomIn: <i className='pi pi-plus' aria-hidden />,
        help: <i className='pi pi-question-circle' aria-hidden />
    },
    controlsFollowViewportInsets: false
};
