# Agent Guidelines for ExtraTime

## Project Plans

### Plan Location
- All plans are stored in `.claude/plan/` directory
- MVP plan: `.claude/plan/mvp-plan.md`
- Phase-specific detailed plans: `.claude/plan/phase-1-detailed.md`, `.claude/plan/phase-2-detailed.md`, etc.
- **When creating a plan in plan mode, always save it to a `.md` file in this directory**

## Code Generation Rules

### C# Classes
- **ALWAYS use `sealed` modifier** on classes unless inheritance is explicitly needed
- Base classes and abstract classes should NOT be sealed
- Entity base classes are exceptions (they need inheritance)

### Project Structure
- Clean Architecture: Domain → Application → Infrastructure → API
- Minimal APIs instead of Controllers
- Mediator source generator for CQRS

### Examples

```csharp
// CORRECT - sealed by default
public sealed class UserService
{
    // ...
}

// CORRECT - record for DTOs
public sealed record LoginRequest(string Email, string Password);

// CORRECT - base class not sealed (needs inheritance)
public abstract class BaseEntity
{
    public Guid Id { get; set; }
}

// WRONG - missing sealed
public class UserService  // ❌ Should be sealed
{
}
```
