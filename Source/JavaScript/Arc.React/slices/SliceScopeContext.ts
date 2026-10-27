// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { SliceScope } from './SliceScope.js';

/**
 * The React context holding the nearest {@link SliceScope}; undefined outside of `<Arc>` and `<Slice>`.
 */
export const SliceScopeContext = React.createContext<SliceScope | undefined>(undefined);
