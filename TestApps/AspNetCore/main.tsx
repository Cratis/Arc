// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { Arc } from '@cratis/arc.react';
import { App } from './App';

createRoot(document.getElementById('root')!).render(
    <StrictMode>
        <Arc>
            <App />
        </Arc>
    </StrictMode>,
);
