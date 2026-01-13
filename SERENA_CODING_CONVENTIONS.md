# Serena Coding Conventions Summary

This document summarizes Serena memory about coding conventions in the Security Dashboard project, with
Result<T> fluent railway (railroad) patterns, minimal try/catch usage, and guidance for shifting from
procedural to functional style.

## Core conventions (from Serena memory)

- Use Result<T> and Maybe for error handling instead of exceptions.
- Prefer fluent chains with Ensure/Bind/Map/Tap (railway-oriented programming).
- Favor immutability and minimize mutable state.
- Use dependency injection; depend on interfaces.
- Follow naming conventions: private fields use underscore, methods use PascalCase.
- Add XML documentation for public APIs.
- Keep methods static if they do not rely on instance state.
- Keep code SOLID and focused (small interfaces, single responsibility).

## Fluent railroad (railway) style with Result<T>

The project uses CSharpFunctionalExtensions to keep error handling explicit and composable.
Below are examples of fluent Result<T> chains that avoid try/catch and keep control flow linear.

Example 1: Validate, map, and persist

```csharp
Result<UserDto> result =
    ParseUser(input)
        .Ensure(user => user.Email.IsValid(), "Invalid email")
        .Bind(user => repository.Save(user))
        .Map(saved => mapper.ToDto(saved))
        .Tap(dto => logger.Info("Saved user"));
```

Example 2: Orchestrate multi-step workflow

```csharp
Result<ScanSummary> result =
    workspaceContext.GetCurrent()
        .Bind(workspace => scanService.Run(workspace))
        .Bind(scan => resultCache.Store(scan))
        .Map(scan => summaryFactory.Create(scan));
```

## Minimal try/catch guidance

- Use try/catch only at system boundaries where exceptions are unavoidable
  (third-party SDKs, IO, process or network calls).
- Wrap exception boundaries into Result.Try or an equivalent adaptor and
  return Result<T> to the caller.
- Propagate errors through Result<T> chains instead of throwing.

## Procedural to functional conversion checklist

1) Replace "early return on error" with Result<T> and Ensure.
2) Replace nested if/else or stateful steps with Bind/Map/Tap chains.
3) Return Result<T> from service methods that can fail.
4) Keep side effects in Tap and business rules in Ensure.
5) Convert mutable temporary variables into mapped transforms.
6) Preserve DI and interface boundaries, but make methods pure where possible.

## Notes for reuse in other clients

- Use CSharpFunctionalExtensions (or a similar Result/Maybe library).
- Keep domain logic in Result<T> chains, isolate side effects.
- Write small composable functions that return Result<T>.
