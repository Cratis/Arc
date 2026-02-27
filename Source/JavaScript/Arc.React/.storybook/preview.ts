// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Preview } from "@storybook/react";
import './stories.css';

const preview: Preview = {
  parameters: {
    backgrounds: {
      default: 'dark',
      values: [
        {
          name: 'dark',
          value: '#1a1a1a',
        },
        {
          name: 'light',
          value: '#f5f5f5',
        },
      ],
    },
    controls: {
      matchers: {
        color: /(background|color)$/i,
        date: /Date$/i,
      },
    },
    docs: {
      source: {
        type: 'code',
      },
    },
  },
};

export default preview;
