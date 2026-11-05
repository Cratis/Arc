---
title: Cratis Arc Storybook setup with story components
description: Set up Cratis Arc story components in React Storybook stories with a shared stylesheet, reusable layouts, and explicit dark or light theme selection.
---

Cratis Arc story components provide shared layouts for React stories in Storybook. Import their stylesheet once, then use the components to give stories the same spacing and card styles. Your Storybook configuration selects the dark or light palette.

<a id="overview"></a>
<a id="motivation"></a>

Use these wrappers when several stories repeat the same container or section styling. The shared stylesheet lets you change that presentation in one place without changing the component each story demonstrates.

## Getting started

Start with an existing React Storybook project that has `@cratis/arc.react` installed. Load the stylesheet once, in your Storybook preview; the components do not import it themselves:

```ts
// .storybook/preview.ts
import '@cratis/arc.react/stories/styles.css';
```

Then import the components you need in your story files. This example assumes your story already defines the `Story` type and imports `MyComponent`:

```tsx
import {
    StoryContainer,
    StorySection,
    StoryGrid,
    StoryBadge,
    StoryDivider,
} from '@cratis/arc.react/stories';

export const MyStory: Story = {
    render: () => (
        <StoryContainer size='md' asCard>
            <h1>My Component</h1>
            <MyComponent />
        </StoryContainer>
    ),
};
```

With the stylesheet loaded, `asCard` gives this container a background, border, and shadow. If the wrapper has no card styling, check the preview import before adding styles to the story itself.

<a id="solution"></a>
<a id="consistency"></a>
<a id="reusable-patterns"></a>

## Choose a story layout

`StoryContainer` wraps a story with centered content and padding. Use `StorySection` to separate related examples, or `StoryGrid` to arrange examples in a responsive grid. `StoryBadge` labels a status, while `StoryDivider` separates groups with a horizontal line.

<a id="beautiful-by-default"></a>

### Shared styles

The package stylesheet supplies the spacing, borders, and colors used by these wrappers. Override its CSS variables when you need different shared styles rather than repeating those values in each story.

<a id="automatic-theme-adaptation"></a>

## Match the Storybook theme

The stylesheet defaults to a dark palette. Set `data-theme="light"` on an ancestor to select the light palette, or `data-theme="dark"` to select dark. Changing Storybook's background alone does not switch these variables. Follow [theme support](theme-support.md#switching-themes) to connect the backgrounds toolbar to the palette.

## What's included

- [Components](components.md) covers the layout components and their props.
- [Styling](styling.md) lists the CSS variables and utility classes to override.
- [Theme support](theme-support.md) explains palette selection and custom colors.

## Examples

For examples combining containers, sections, grids, and badges, see `StoryContainer.stories.tsx` in the source code.
