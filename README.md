# Yugo

Yugo is a grid-based puzzle game built with MonoGame. The core logic is isolated in a standalone library so the gameplay rules can be tested without the rendering layer.

## Structure
- `src/Yugo.Core`: Game rules, entities, and level logic.
- `src/Yugo.Game`: MonoGame desktop app.
- `tests/Yugo.Core.Tests`: Unit tests for the core library.

## Tooling
- Format: `dotnet format --verify-no-changes`
- Lint: .NET analyzers via `Directory.Build.props`
- Tests: `dotnet test Yugo.slnx`

## First-time setup
1. Restore tools: `dotnet tool restore`
2. Restore packages: `dotnet restore Yugo.slnx`

## Pre-commit
Pre-commit hooks run formatting and tests:
- `scripts/format.sh`
- `scripts/test.sh`

Install hooks:
- `pre-commit install`
