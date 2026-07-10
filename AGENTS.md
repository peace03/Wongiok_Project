# Project Agent Instructions

This Unity project uses Karpathy-inspired coding guidelines to keep AI-assisted edits small, clear, and verifiable.

## 1. Think Before Coding

- State assumptions explicitly. Ask when the request has multiple plausible meanings.
- Surface tradeoffs before implementation when they affect design, behavior, or project structure.
- Prefer the simplest interpretation that satisfies the user's stated goal.

## 2. Simplicity First

- Implement only what was requested.
- Avoid speculative abstractions, extra configuration, and unused extension points.
- Keep Unity scripts straightforward and readable.
- If a change starts getting large, look for the smaller version before continuing.

## 3. Surgical Changes

- Touch only files required by the task.
- Match the existing C# and Unity style in nearby scripts.
- Do not refactor unrelated code or reformat unrelated sections.
- Remove only unused code introduced by the current change.
- Mention unrelated dead code or risks instead of deleting them.

## 4. Goal-Driven Execution

- Convert work into clear success criteria before making non-trivial changes.
- Verify with the narrowest useful check available, such as compilation, Unity tests, or focused code inspection.
- For bug fixes, prefer reproducing the bug first when practical.
- Stop and ask if the success condition is unclear.
- Before modifying more than one file, provide a brief implementation plan and wait for confirmation.

## 5. Unity-Specific Rules

- Preserve serialized fields, `[SerializeField]`, public inspector fields, and prefab-facing APIs unless explicitly asked to change them.
- Do not rename MonoBehaviour classes, files, serialized fields, animation parameters, input action names, tags, layers, or scene object references unless required.
- Avoid introducing new Manager, Singleton, Service Locator, or global state patterns unless the project already uses them nearby.
- Prefer existing project architecture and naming over new patterns.
- Do not use `FindObjectOfType`, `GameObject.Find`, or broad scene searches unless already used nearby or explicitly requested.
- Keep runtime logic out of `Update()` unless frame-by-frame behavior is necessary.
- For stat, combat, inventory, and UI systems, separate data calculation from presentation/UI updates.
- When changing gameplay formulas, state the formula before editing and verify with at least one concrete example.
