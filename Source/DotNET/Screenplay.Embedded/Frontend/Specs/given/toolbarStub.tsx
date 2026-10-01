// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ReactNode } from 'react';

// The toolbar from @cratis/components as these specs need it: a named toolbar with buttons that carry
// their title as their accessible name.
export const Toolbar = ({ children, 'aria-label': label }: { children: ReactNode; orientation?: string; 'aria-label'?: string }) => (
    <div role='toolbar' aria-label={label}>{children}</div>
);

export const ToolbarButton = ({ title, icon }: { title?: string; icon?: string; tooltipPosition?: string }) => (
    <button type='button' title={title} aria-label={title}><i className={icon} /></button>
);
