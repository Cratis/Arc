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
inside the package, and a consumer loads it once:

```ts
// .storybook/preview.ts
import '@cratis/arc.react/stories/styles.css';
```

Dark is the default palette. Setting `data-theme="light"` on `<html>` or `<body>` switches every variable to
the light palette, and `data-theme="dark"` switches it back.

### Why the components do not import it themselves

They did, briefly, and it broke the build. A stylesheet import in the source becomes a stylesheet import in
the emitted JavaScript, and this package has two builders that disagree about whether that resolves:
`yarn build` runs `tsc -b` and then Rollup, and only Rollup copies the asset next to the emitted JS. When
another TypeScript project holds a **project reference** to this one - `Arc.React.MVVM` does, and so can any
consuming application - `tsc -b` builds this project directly and no package script runs at all, leaving
`import './stories.css'` pointing at a file nothing copied.

So nothing in the emitted JavaScript points at a stylesheet, and the export subpath is the only way in.
`for_packageManifest/when_shipping_the_story_kit_stylesheet.tsx` fails if an import creeps back in, and so
does the build.

Do not add `.story-*` rules to `.storybook/stories.css` either. That file is this repository's own Storybook
chrome and never reaches a consumer; it imports `stories.css` so the local preview renders through the same
rules the package publishes, which is the in-repo spelling of the one import above. The same specs fail if
the two drift apart, or if a rendered class name loses its rule.

## Examples

See `StoryContainer.stories.tsx` for comprehensive examples of all components and patterns.
