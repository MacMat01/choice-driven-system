# Weighted Draw System

`Weighted Draw System` is a Unity package that combines:

- `ProbabilityEngine` for condition-aware weighted selection
- `SchemaImporter` for schema-driven CSV/JSON import

## Authors

- MacMat01
- Tennents02
- dianila68

## Package Structure

- `Runtime/ProbabilityEngine` - runtime weighted selection engine
- `Runtime/SchemaImporter` - runtime data import and parsing
- `Tests/EditMode/ProbabilityEngine` - Edit Mode tests for the engine
- `Tests/EditMode/SchemaImporter` - Edit Mode tests for the importer and parsers
- `Tests/EditMode/Fixtures` - test-only CSV/JSON and schema assets
- `Documentation~` - package documentation for both modules

## Documentation

- `Documentation~/ProbabilityEngine.md`
- `Documentation~/SchemaImporter.md`

## Install

This package is intended for Unity Package Manager, local package import, or Git-based package installation.

## Notes

- The package manifest is `package.json`.
- Test fixtures are intentionally separate from runtime code.
- The documentation and tests are the source of truth for current behavior.


