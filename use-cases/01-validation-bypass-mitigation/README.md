# 01 — Validation Bypass Mitigation

Original work and paper: https://github.com/Ikeragnell/shaclExploits

## Context
With SHACL, a common pattern is to restrict a value to an enumeration. We could achieve this with `sh:in`, but that is **static**—changing allowed values means editing the shapes. A **dynamic** alternative is to keep the allowed values in a **dedicated named graph** and validate against it. However, if validation **flattens** the submission and that reference graph into a **single data graph**, the graph-of-origin (provenance) is lost.

For example, a social-security declaration may only accept codes listed in an official document (e.g., “Annex 9”).

## Threat
An attacker forges an enumeration triple in the input graph so that an otherwise invalid value **passes** validation.

## Defense
**Preserve provenance:** Write rules that only accept enumeration values **when asserted in a trusted graph** (e.g., `ex:annex9`). Optionally, **flag** definitions that appear outside the trusted graph.

## Files
- `annex9.ttl` — the official/valid enumeration.
- `forged_enumeration.ttl` — the attacker’s forged enumeration in `ex:attacker`.
- `shapes_vulnerable.ttl` — a vulnerable shape that can be bypassed when provenance is lost.
- `shapes_secure.trig` — secure shapes that require values to originate from `ex:annex9`.
- `data_graph.ttl` — a **single data graph** where `annex9.ttl` and `forged_declaration.ttl` have been merged (provenance lost).
- `data_dataset.trig` — a dataset combining `annex9.ttl` and `forged_enumeration.ttl` as separate **named graphs**.

## Expected
- `data_graph.ttl` + `shapes_vulnerable.ttl` → **PASSES** under SHACL (validation bypassed).
- `data_dataset.trig` + `shapes_secure.trig` → **FAILS** under SHACL-DS (bypass blocked).

## How to run

### Prerequisite: 

Build the CLI from this directory:
```
dotnet build ../../src/SHACL-DS.cli/SHACL-DS.cli.csproj
```

### Running
```bash
dotnet run --project ../../src/SHACL-DS.cli  --data data_graph.ttl --shapes shapes_vulnerable.ttl
```

```bash
dotnet run --project ../../src/SHACL-DS.cli  --data data_dataset.trig  --shapes shapes_secure.trig 
```

One can observe the expected results.