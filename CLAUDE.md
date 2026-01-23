# Claude Code Guidelines for ExtraTime

## Project Plans

### Plan Location
- All plans are stored in `.claude/plan/` directory
- MVP plan: `.claude/plan/mvp-plan.md`
- Phase-specific detailed plans: `.claude/plan/phase-1-detailed.md`, `.claude/plan/phase-2-detailed.md`, etc.
- **When creating a plan in plan mode, always save it to a `.md` file in this directory**

## C# Coding Standards

### Classes
- **Seal all classes by default** - Use `sealed` modifier on all classes unless inheritance is explicitly required
- Use primary constructors where applicable
- Prefer records for DTOs and value objects

### Architecture
- **Clean Architecture** with Domain, Application, Infrastructure, API layers
- **Minimal APIs** - No controllers, use endpoint classes
- **Mediator pattern** - Use Mediator source generator (not MediatR)
- **Vertical Slice** organization within each layer when applicable

### Naming Conventions
- PascalCase for public members
- _camelCase for private fields
- Async suffix for async methods

### General
- Use file-scoped namespaces
- Prefer `var` when type is obvious
- Use nullable reference types
