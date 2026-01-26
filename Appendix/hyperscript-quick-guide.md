---
order: 2
icon: stack
label: Appendix C - Hyperscript Quick Guide
meta:
title: "Hyperscript Quick Guide"
---
# Appendix C: Hyperscript Quick Guide

Hyperscript is a companion scripting language for htmx that provides a readable, English-like syntax for client-side interactions. This appendix covers the essential patterns for using Hyperscript with htmx applications.

---

## C.1 Introduction

### What is Hyperscript?

Hyperscript is an expressive scripting language designed to be embedded directly in HTML. It complements htmx by handling client-side interactions that don't require server round-trips.

### When to Use Hyperscript

| Use Hyperscript For | Use htmx For |
|---------------------|--------------|
| Toggle visibility | Fetch server content |
| Add/remove classes | Form submissions |
| Keyboard shortcuts | CRUD operations |
| Close modals | Search/filter |
| Client-side validation | Load partial views |

### Installation

```html
<script src="https://unpkg.com/hyperscript.org@0.9.12"></script>
```

---

## C.2 Core Syntax

### The `_` Attribute

Hyperscript code goes in the `_` attribute (or `data-script`):

```html
<button _="on click add .active to me">
    Click Me
</button>
```

### Basic Structure

```
on <event> <commands>
```

```html
<!-- Single command -->
<button _="on click toggle .active on me">Toggle</button>

<!-- Multiple commands (use 'then') -->
<button _="on click add .loading to me then wait 1s then remove .loading from me">
    Load
</button>
```

### Targeting Elements

| Target | Syntax | Example |
|--------|--------|---------|
| Self | `me` | `add .active to me` |
| By ID | `#id` | `remove .hidden from #modal` |
| By class | `.class` | `add .highlight to .item` |
| Closest ancestor | `closest <selector>` | `remove closest .card` |
| Parent | `my parent` | `hide my parent` |
| Next sibling | `next <selector>` | `show next .content` |
| Previous sibling | `previous <selector>` | `hide previous .header` |
| Query | `<selector/>` | `set <input/>.value to ''` |

```html
<!-- Target by ID -->
<button _="on click show #details">Show Details</button>

<!-- Target closest ancestor -->
<button _="on click remove closest .notification">Dismiss</button>

<!-- Target next sibling -->
<button _="on click toggle .expanded on next .content">Expand</button>
```

---

## C.3 Common Commands

### Classes: add, remove, toggle

```html
<!-- Add class -->
<button _="on click add .active to me">Activate</button>

<!-- Remove class -->
<button _="on click remove .error from #form">Clear Error</button>

<!-- Toggle class -->
<button _="on click toggle .open on #menu">Toggle Menu</button>

<!-- Toggle between classes -->
<button _="on click toggle between .light and .dark on <body/>">
    Theme
</button>
```

### Attributes: set, remove

```html
<!-- Set attribute -->
<button _="on click set #input.disabled to true">Disable</button>

<!-- Remove attribute -->
<button _="on click remove @disabled from #input">Enable</button>

<!-- Set multiple -->
<button _="on click set #form.dataset.submitted to 'true'">Submit</button>
```

### Visibility: show, hide, toggle

```html
<!-- Show element -->
<button _="on click show #panel">Show</button>

<!-- Hide element -->
<button _="on click hide #panel">Hide</button>

<!-- Toggle visibility -->
<button _="on click toggle the *display of #panel">Toggle</button>

<!-- With animation -->
<button _="on click transition #panel's opacity to 0 then hide #panel">
    Fade Out
</button>
```

### Variables: set, get

```html
<!-- Set variable -->
<div _="on load set $count to 0">
    <button _="on click set $count to $count + 1 then put $count into #counter">
        Increment
    </button>
    <span id="counter">0</span>
</div>

<!-- Get property -->
<button _="on click get #input.value then log it">Log Value</button>
```

### Timing: wait, settle

```html
<!-- Wait before action -->
<button _="on click add .loading then wait 2s then remove .loading">
    Load
</button>

<!-- Wait for htmx settle -->
<div _="on htmx:afterSettle wait 100ms then add .ready">
    Content
</div>
```

### Events: trigger, send

```html
<!-- Trigger event -->
<button _="on click trigger closeModal">Close</button>

<!-- Send event to element -->
<button _="on click send refresh to #data-panel">Refresh</button>
```

### JavaScript: call, js

```html
<!-- Call JavaScript function -->
<button _="on click call alert('Hello!')">Alert</button>

<!-- Inline JavaScript -->
<button _="on click js console.log('clicked') end">Log</button>

<!-- Call with return value -->
<button _="on click call prompt('Name?') then set #name.innerText to it">
    Ask Name
</button>
```

---

## C.4 Event Handling

### Basic Events

```html
<!-- Click -->
<button _="on click add .clicked to me">Click</button>

<!-- Mouse events -->
<div _="on mouseenter add .hover to me on mouseleave remove .hover from me">
    Hover Me
</div>

<!-- Keyboard -->
<input _="on keyup if event.key is 'Enter' trigger submit">

<!-- Form -->
<form _="on submit halt">...</form>
```

### Event Modifiers

| Modifier | Description | Example |
|----------|-------------|---------|
| `once` | Fire only once | `on click once` |
| `debounced` | Debounce | `on keyup debounced at 300ms` |
| `throttled` | Throttle | `on scroll throttled at 100ms` |

```html
<!-- Fire once -->
<button _="on click once add .initialized">Init</button>

<!-- Debounce input -->
<input _="on keyup debounced at 300ms trigger search">

<!-- Throttle scroll -->
<div _="on scroll throttled at 100ms call checkPosition()">
```

### Event Filtering

```html
<!-- Filter by key -->
<input _="on keydown[key=='Escape'] trigger cancel">

<!-- Filter by target -->
<div _="on click[target==me] hide me">
    Click backdrop to close
</div>

<!-- Compound filters -->
<input _="on keydown[ctrlKey and key=='s'] halt trigger save">
```

### Custom Events

```html
<!-- Listen for custom event -->
<div _="on closeModal remove me">Modal Content</div>

<!-- Listen for htmx events -->
<form _="on htmx:afterRequest reset() me">
    <input name="data" />
    <button>Submit</button>
</form>
```

---

## C.5 htmx Integration Patterns

### Modal: Close on Backdrop Click

```html
<div class="modal-backdrop" 
     _="on click if event.target is me trigger closeModal">
</div>

<div class="modal" 
     _="on closeModal remove .modal-backdrop from previous <div/> 
        then remove me">
    Modal content
</div>
```

### Modal: Close on Escape Key

```html
<div class="modal" 
     _="on keydown[key=='Escape'] from window trigger closeModal
        on closeModal remove me">
    Modal content
</div>
```

### Form: Reset After htmx Submit

```html
<form hx-post="/api/create" 
      hx-target="#list" 
      hx-swap="afterbegin"
      _="on htmx:afterRequest reset() me">
    <input name="title" />
    <button>Create</button>
</form>
```

### Form: Focus First Input in Modal

```html
<div class="modal" 
     _="on load set $input to first <input/> in me then $input.focus()">
    <input name="name" />
</div>
```

### Toast: Auto-Dismiss

```html
<div class="toast" 
     _="init wait 5s then transition my opacity to 0 then remove me">
    Success message
</div>
```

### Loading: Button State

```html
<button hx-post="/save" 
        _="on htmx:beforeRequest add .loading to me
           on htmx:afterRequest remove .loading from me">
    <span class="normal">Save</span>
    <span class="loading hidden">Saving...</span>
</button>
```

### Keyboard: Global Shortcuts

```html
<body _="on keydown[key=='/' and not target.matches('input')] 
           halt then call document.getElementById('search').focus()">
```

### Infinite Scroll: Remove Trigger

```html
<div hx-get="/more" 
     hx-trigger="revealed" 
     hx-swap="afterend"
     _="on htmx:afterRequest remove me">
    Loading more...
</div>
```

### Inline Edit: Cancel on Escape

```html
<tr _="on keyup[key=='Escape'] trigger click on #cancel-btn">
    <td>
        <input name="name" />
    </td>
    <td>
        <button type="submit">Save</button>
        <button id="cancel-btn" hx-get="/cancel">Cancel</button>
    </td>
</tr>
```

### Confirmation: Custom Dialog

```html
<button _="on click halt 
           then set result to call confirm('Delete?')
           then if result trigger confirmed">
    Delete
</button>

<form hx-delete="/item/5" 
      hx-trigger="confirmed from previous <button/>">
</form>
```

---

## C.6 Quick Reference

### Syntax Cheat Sheet

| Action | Syntax |
|--------|--------|
| Add class | `add .class to element` |
| Remove class | `remove .class from element` |
| Toggle class | `toggle .class on element` |
| Show element | `show element` |
| Hide element | `hide element` |
| Remove element | `remove element` |
| Set property | `set element.prop to value` |
| Set variable | `set $var to value` |
| Wait | `wait 1s` |
| Trigger event | `trigger eventName` |
| Call function | `call functionName()` |
| Log | `log value` |
| Halt event | `halt` |
| Conditional | `if condition action` |

### Common Patterns

| Pattern | Code |
|---------|------|
| Close on Escape | `on keydown[key=='Escape'] remove me` |
| Close on backdrop | `on click if event.target is me remove me` |
| Toggle sibling | `on click toggle .open on next .content` |
| Auto-dismiss | `init wait 5s then remove me` |
| Reset form | `on htmx:afterRequest reset() me` |
| Focus input | `on load set $i to first <input/> in me then $i.focus()` |
| Debounce | `on keyup debounced at 300ms trigger search` |

### htmx Events

| Event | Use Case |
|-------|----------|
| `htmx:beforeRequest` | Add loading state |
| `htmx:afterRequest` | Remove loading state, reset form |
| `htmx:afterSettle` | Initialize components |
| `htmx:afterSwap` | Post-swap actions |
| `htmx:confirm` | Custom confirmation |

---

*For more details, see the [Hyperscript documentation](https://hyperscript.org).*
