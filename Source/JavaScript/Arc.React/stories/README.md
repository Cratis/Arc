# Story Components

Common components and utilities for creating beautiful Storybook stories that automatically adapt to dark and light modes.

## Documentation

For complete documentation on using Story Components, see the [Story Components Documentation](../../../Documentation/frontend/react/stories/index.md).

The documentation covers:

- **Overview**: Motivation and benefits of using Story Components
- **Components**: Detailed reference for all available components
- **Styling**: CSS variables and utility classes
- **Theme Support**: Automatic dark/light mode adaptation

## Quick Start

```tsx
import { StoryContainer, StorySection, StoryBadge } from '@cratis/arc.react/stories';

export const MyStory: Story = {
  render: () => (
    <StoryContainer size="md" asCard>
      <h1>My Component</h1>
      <MyComponent />
    </StoryContainer>
  ),
};
```

## Styling

The components render class names; `stories.css` in this folder is what those class names mean. It ships
inside the package and is side-effect imported by the components, so importing anything from
`@cratis/arc.react/stories` brings the styling with it - there is nothing to wire up.

Import it explicitly only when the order matters - to load the kit's tokens before your own theme overrides
them, for instance:

```ts
// .storybook/preview.ts
import '@cratis/arc.react/stories/styles.css';
```

Dark is the default palette. Setting `data-theme="light"` on `<html>` or `<body>` switches every variable to
the light palette, and `data-theme="dark"` switches it back.

Do not add `.story-*` rules to `.storybook/stories.css`. That file is this repository's own Storybook chrome
and never reaches a consumer; it imports `stories.css` so the local preview renders through the same rules
the package publishes. `for_packageManifest/when_shipping_the_story_kit_stylesheet.tsx` fails if the two
drift apart, or if a rendered class name loses its rule.

## Examples

See `StoryContainer.stories.tsx` for comprehensive examples of all components and patterns.
