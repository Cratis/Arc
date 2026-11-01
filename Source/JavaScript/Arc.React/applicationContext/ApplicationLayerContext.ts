// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';

/**
 * Holds the id of the open dialog that the components rendered within belong to; undefined for the page.
 */
export const ApplicationLayerContext = React.createContext<string | undefined>(undefined);
