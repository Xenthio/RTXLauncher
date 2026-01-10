# Fazor Default Styles

This stylesheet provides s&box-compatible default styles to make XGUI themes work correctly with Fazor.

## Purpose

In s&box, everything uses `display: flex` by default. This is fundamental to how XGUI themes are designed. Without these defaults, XGUI themes won't render correctly because they assume flexbox layout everywhere.

## What This Does

```scss
* {
    display: flex;
    flex-direction: column;
    position: relative;
    box-sizing: border-box;
}
```

This matches s&box's default behavior:
- **All elements are flex containers** by default (like s&box)
- **Column layout by default** (vertical stacking)
- **Relative positioning** (allows absolute children)
- **Border-box sizing** (consistent box model)

## Exceptions

Some elements override the flex default to behave naturally:

- **Text elements** (`span`, `label`, `p`, `h1-h6`) use `display: inline`
- **Form controls** (`input`, `textarea`, `select`, `button`) use `display: inline-block`
- **Images** use `display: inline-block`

## Usage

### Automatic (Recommended)

Import this at the start of your main stylesheet or component:

```scss
@import "/themes/Fazor.Defaults.scss";

.my-component {
    // Your styles here
}
```

### With XGUI Themes

XGUI themes now work correctly without manual flex declarations:

```razor
@attribute [StyleSheet("/themes/Fazor.Defaults.scss")]
@attribute [StyleSheet("/XGUI/DefaultStyles/OliveGreen.scss")]
```

Or import it in your SCSS:

```scss
@import "/themes/Fazor.Defaults.scss";
@import "/XGUI/DefaultStyles/OliveGreen.scss";

// Your custom styles
```

## Why This Matters for XGUI

XGUI window decorations and layout rely on flexbox:

- **Window.TitleBar** uses flex to arrange title, icon, and control buttons
- **TitleElements** uses `flex-grow: 1` for spacing
- **Window content** uses `flex-direction: column` for vertical layout
- All **layout containers** expect flex behavior

Without these defaults, you'd need to add `display: flex` to every single element, which defeats the purpose of using XGUI themes as-is.

## Comparison

**Without Fazor.Defaults.scss:**
```scss
.my-window {
    display: flex;  // Need to add manually
    flex-direction: column;
    
    .header {
        display: flex;  // Need to add manually
        flex-direction: row;
    }
    
    .content {
        display: flex;  // Need to add manually
        flex-direction: column;
    }
}
```

**With Fazor.Defaults.scss:**
```scss
.my-window {
    // flex is already default!
    
    .header {
        flex-direction: row;  // Just override the direction
    }
    
    .content {
        // Already flex column by default
    }
}
```

This makes your code cleaner and XGUI themes work exactly like they do in s&box.
