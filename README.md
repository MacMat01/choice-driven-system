# Weighted Draw System

This repository contains a Unity project plus the package source at `Packages/com.macmat01.weighted-draw-system`.

The package is a **game-agnostic**, **UPM-friendly** framework for:

- editor-time CSV compilation
- generic row deserialization
- weighted random selection
- custom game-state condition evaluation
- sample-driven usage instead of hardcoding one specific game schema

Some legacy code is preserved under `_Old` for migration support and is explicitly marked deprecated.

## Recommended package usage

If you are using the package as a consumer, start with the package-level documentation:

- `Packages/com.macmat01.weighted-draw-system/README.md`

That document explains:

- end-to-end flow from CSV file to runtime weighted draw
- editor lifecycle (`OnValidate`, column sync, compile)
- a practical tutorial for creating your own row type and authoring asset
- runtime usage patterns with `WeightedDrawEngine<TEntry, TContext>`
- migration differences between current APIs and deprecated `_Old` APIs

## Package layout

Current package areas:

- `Packages/com.macmat01.weighted-draw-system/Runtime`
- `Packages/com.macmat01.weighted-draw-system/Editor`
- `Packages/com.macmat01.weighted-draw-system/Tests`
- `Packages/com.macmat01.weighted-draw-system/_Old`

## Example use case

The package is meant for systems such as:

- loot tables
- dialogue/event cards
- encounter tables
- shop inventories
- any other weighted CSV-driven system

The event-card sample shows how to model your own schema while keeping the framework generic.

## Installation

### Unity Package Manager

Use the package path URL:

```text
https://github.com/MacMat01/weighted-draw-system.git?path=/Packages/com.macmat01.weighted-draw-system
```

If you want a specific revision, append `#branch`, `#tag`, or `#commit`.

### Local development

Keep the package in this repository at:

```text
Packages/com.macmat01.weighted-draw-system
```

## Testing

Edit Mode tests live under:

- `Packages/com.macmat01.weighted-draw-system/Tests/EditMode`

They cover:

- CSV parsing
- weighted selection
- condition evaluation
- editor-time authoring and compilation

## License

See `LICENSE`.
