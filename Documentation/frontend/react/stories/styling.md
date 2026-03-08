# Styling Reference

The story components use a comprehensive set of CSS variables and utility classes for consistent theming.

## CSS Variables

All variables automatically update based on the current Storybook theme (dark/light mode).

### Colors

#### Text Colors

- `--color-text`: Primary text color
- `--color-text-secondary`: Secondary text color for less important content
- `--color-text-muted`: Muted text color for hints and labels

#### Background Colors

- `--color-background`: Main background color
- `--color-background-secondary`: Secondary background (cards, inputs)
- `--color-background-tertiary`: Tertiary background (hover states)

#### Border Colors

- `--color-border`: Standard border color
- `--color-border-light`: Lighter border color for subtle divisions

#### Brand Colors

- `--color-primary`: Primary brand color
- `--color-primary-hover`: Primary color hover state

#### Semantic Colors

- `--color-success`: Success state color (typically green)
- `--color-warning`: Warning state color (typically yellow/orange)
- `--color-error`: Error state color (typically red)
- `--color-info`: Info state color (typically blue)

### Spacing

Consistent spacing values for padding and margins:

- `--space-xs`: 0.25rem (4px)
- `--space-sm`: 0.5rem (8px)
- `--space-md`: 1rem (16px)
- `--space-lg`: 1.5rem (24px)
- `--space-xl`: 2rem (32px)
- `--space-2xl`: 3rem (48px)

### Visual Effects

#### Border Radius

- `--radius-sm`: Small border radius
- `--radius-md`: Medium border radius
- `--radius-lg`: Large border radius

#### Box Shadows

- `--shadow-sm`: Subtle shadow for slight elevation
- `--shadow-md`: Medium shadow for cards and modals
- `--shadow-lg`: Large shadow for prominent elements

### Typography

- `--font-sans`: Sans-serif font family for UI text
- `--font-mono`: Monospace font family for code

## Utility Classes

Pre-built CSS classes you can use directly in your stories.

### Container Classes

- `.story-container`: Standard container with medium width
- `.story-container-sm`: Small container (600px max width)
- `.story-container-lg`: Large container (1400px max width)

### Layout Classes

- `.story-section`: Section wrapper with consistent vertical spacing
- `.story-grid`: Responsive grid layout
- `.story-card`: Card styling with background, border, and shadow

### Visual Elements

- `.story-divider`: Horizontal divider line

### Badge Classes

- `.story-badge`: Base badge style
- `.story-badge-success`: Green success badge
- `.story-badge-warning`: Yellow/orange warning badge
- `.story-badge-error`: Red error badge
- `.story-badge-info`: Blue info badge

## Usage Examples

### Using CSS Variables

```tsx
<div style={{ 
  padding: 'var(--space-lg)', 
  backgroundColor: 'var(--color-background-secondary)',
  borderRadius: 'var(--radius-md)',
  boxShadow: 'var(--shadow-sm)'
}}>
  Custom styled content
</div>
```

### Using Utility Classes

```tsx
<div className="story-container">
  <div className="story-card">
    <h2>Card Title</h2>
    <p>Card content</p>
  </div>
</div>
```

### Combining Variables and Classes

```tsx
<div className="story-grid">
  <div 
    className="story-card" 
    style={{ borderColor: 'var(--color-primary)' }}
  >
    Custom card with primary border
  </div>
</div>
```

## Best Practices

1. **Prefer CSS Variables**: Use variables instead of hard-coded colors for theme compatibility
2. **Use Utility Classes**: Leverage utility classes for common patterns to ensure consistency
3. **Semantic Colors**: Use semantic color variables (`--color-success`, etc.) for meaningful states
4. **Consistent Spacing**: Stick to the spacing scale (`--space-*`) for consistent rhythm
5. **Theme Testing**: Always test your stories in both dark and light modes
