// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import 'primeicons/primeicons.css';
import '@cratis/components/tokens';
import '@cratis/components/styles';
import '@cratis/components/theme';
import '@cratis/scene/styles';
import '@cratis/event-models/theme';
import '@cratis/event-models/styles';
import './app.css';
import { App } from './App';

// The board's own chrome - header labels, pills, row labels - is drawn in colors made for a dark surface,
// so the viewer is dark only, exactly as Cratis Studio's board is.
document.documentElement.classList.add('cratis-dark');

const container = document.getElementById('root');
if (!container) throw new Error('The viewer host element is missing from the page');

createRoot(container).render(
    <StrictMode>
        <App />
    </StrictMode>);
