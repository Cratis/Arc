// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { ArcContext, ArcConfiguration } from '../../../../ArcContext';

export class a_command_form_fields_context {
    arcConfig: ArcConfiguration;

    constructor() {
        this.arcConfig = {
            microservice: 'test-microservice',
            apiBasePath: '/api',
            origin: 'https://example.com',
            httpHeadersCallback: () => ({})
        };
    }

    createWrapper() {
        return ({ children }: { children: React.ReactNode }) => React.createElement(
            ArcContext.Provider,
            { value: this.arcConfig },
            children
        );
    }
}
