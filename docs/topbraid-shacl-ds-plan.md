# SHACL-DS Implementation Plan for TopBraid

## Context

This document describes the plan to implement SHACL-DS (SHACL for Datasets) on top of the TopBraid SHACL Java library (`org.topbraid/shacl`, Apache Jena-based). The implementation follows the same semantics as the dotNetRDF SHACL-DS implementation but exploits TopBraid's native dataset-aware architecture.

---

## Key Architectural Insight

**TopBraid already operates on a full Jena `Dataset` internally.** Its `ValidationEngine` takes a `Dataset` where the data graph is the default model and the shapes are a named graph. SPARQL constraints already execute against this `Dataset` via `QueryExecution`, so all named graphs are inherently accessible.

This is fundamentally different from dotNetRDF, where SPARQL had to be intercepted and re-routed to a `LeviathanQueryProcessor(dataset)`. In TopBraid, **we only need to set up the correct dataset view** before calling the standard engine.

---

## What SHACL-DS Adds

| Feature | Description |
|---|---|
| `shds:targetGraph` | Declares which named data graph a shapes graph validates |
| `shds:targetGraphPattern` | Regex-based graph name selection |
| `shds:targetGraphExclude` / `shds:targetGraphPatternExclude` | Exclusion from selection |
| `shds:all`, `shds:named`, `shds:default` | Special IRI expansions |
| `shds:targetGraphCombination` | Union / intersection / difference of graphs |
| Dataset view transformation | Focus graph → default; original default → `shds:default` named graph |
| Result annotations | `shds:sourceShapesGraph`, `shds:focusGraph` on each `sh:ValidationResult` |
| Self-validation | Shapes dataset validated against meta-schema before use |

---

## Package Structure

```
src/main/java/org/topbraid/shacl/ds/
├── ShapesDatasetEngine.java          # Main orchestrator
├── DatasetViewTransformer.java       # Dataset view for each focus graph
├── TargetGraphSelector.java          # Resolves shds:targetGraph / Pattern / Exclude
├── vocabulary/
│   └── SHDS.java                     # Vocabulary constants (namespace https://w3id.org/shacl-ds#)
├── combinations/
│   ├── GraphCombination.java         # Abstract base
│   ├── AtomCombination.java          # Leaf: a single named graph
│   ├── OrCombination.java            # Union of models
│   ├── AndCombination.java           # Intersection of triples
│   └── MinusCombination.java         # Set difference of triples
└── validation/
    └── DatasetValidationReport.java  # Annotated report wrapper
```

---

## Step-by-Step Implementation Plan

### Step 1 — `SHDS.java` (Vocabulary)

Define all SHACL-DS vocabulary constants as static `Property` and `Resource` fields (same pattern as `SH.java` in TopBraid):

```java
public class SHDS {
    public static final String BASE_URI = "https://w3id.org/shacl-ds#";
    public static final String NS = BASE_URI;

    public static final Resource all;
    public static final Resource named;
    public static final Resource default_;      // shds:default (reserved Java keyword)

    public static final Property targetGraph;
    public static final Property targetGraphPattern;
    public static final Property targetGraphExclude;
    public static final Property targetGraphPatternExclude;
    public static final Property targetGraphCombination;
    public static final Property or;
    public static final Property and;
    public static final Property minus;
    public static final Property sourceShapesGraph;
    public static final Property focusGraph;
}
```

---

### Step 2 — `DatasetViewTransformer.java`

Creates a `Dataset` view for a given (dataset, targetGraphURI) pair:

**Transformation rules** (from SHACL-DS spec):
1. The original default graph is stored as a named graph under `shds:default`
2. The focus graph (named graph) becomes the new default graph
3. All other named graphs remain accessible

**Jena implementation strategy:**

Use `DatasetGraphWrapper` or build a new `DatasetGraph` that overrides `getDefaultGraph()` to return the focus graph's backing graph. No data is physically copied — it is a **view**.

```java
public class DatasetViewTransformer {
    public static Dataset createView(Dataset original, String focusGraphURI) {
        // 1. Wrap original DatasetGraph
        // 2. Override getDefaultGraph() → focus graph
        // 3. addNamedGraph(SHDS.NS + "default", original.getDefaultModel().getGraph())
        // 4. Return wrapped Dataset
    }

    public static Dataset restoreView(Dataset view) {
        // Unwrap back to original — or just discard view
    }
}
```

---

### Step 3 — `TargetGraphSelector.java`

Resolves the target graphs declared in a shapes dataset against a data dataset:

```
Input:  shapes dataset (TriG), data dataset (TriG or multiple graphs)
Output: List<Pair<String shapesGraphURI, String targetGraphURI>>
```

**Algorithm** (mirrors dotNetRDF's SPARQL query approach):

1. For each named graph `S` in the shapes dataset:
   - Find all `shds:targetGraph` triples → direct graph IRIs
   - Find all `shds:targetGraphPattern` triples → regex, match against data graph names
   - Expand `shds:all` → all data graphs + default
   - Expand `shds:named` → all named data graphs
   - Expand `shds:default` → default data graph
2. Apply exclusions (`shds:targetGraphExclude`, `shds:targetGraphPatternExclude`)
3. Return the resulting pairs

---

### Step 4 — `GraphCombination.java` hierarchy

Abstract base and four concrete implementations:

```
GraphCombination (abstract)
    ├── AtomCombination     → returns dataset.getNamedModel(uri) directly
    ├── OrCombination       → Model union (model.add() all args)
    ├── AndCombination      → Triple-level intersection across all args
    └── MinusCombination    → Triple-level set difference (exactly 2 operands)
```

**Jena note:** Use `ModelFactory.createUnion(m1, m2)` for Or, manual triple iteration for And/Minus.

Special IRI expansion (same as dotNetRDF):
- `shds:all` → all named graphs + default
- `shds:named` → all named graphs
- `shds:default` → default graph

---

### Step 5 — `ShapesDatasetEngine.java` (Main Orchestrator)

```
ShapesDatasetEngine.validate(Dataset shapesDataset, Dataset dataDataset)
│
├── 1. Self-validate shapesDataset
│       ValidationUtil.validateModel(shapesDataset.union, metaShapesModel, false)
│       Abort if not conforms
│
├── 2. TargetGraphSelector.resolve(shapesDataset, dataDataset)
│       → List<Pair<shapesGraphURI, targetGraphURI>>
│
├── 3. For each (shapesGraph, targetGraph):
│       view = DatasetViewTransformer.createView(dataDataset, targetGraph)
│       shapesModel = shapesDataset.getNamedModel(shapesGraph)
│       engine = ValidationUtil.createValidationEngine(view.getDefaultModel(), shapesModel, false)
│       // Override dataset in engine to use full view (not just default model)
│       engine.setDataset(view)
│       report = engine.validateAll()
│       Annotate results: shds:sourceShapesGraph = shapesGraph, shds:focusGraph = targetGraph
│
├── 4. TargetGraphSelector.resolveGraphCombinations(shapesDataset, dataDataset)
│       → List<Pair<shapesGraphURI, GraphCombination>>
│
├── 5. For each (shapesGraph, combination):
│       combinedModel = combination.toModel(dataDataset)
│       Validate combinedModel against shapesGraph
│       Annotate results with combination definition triples
│
└── 6. Merge all results into a single sh:ValidationReport
```

**Key issue to resolve:** `ValidationUtil.createValidationEngine()` calls `ARQFactory.get().getDataset(dataModel)` internally, which wraps just the default model. We need the engine's internal dataset to be the **full view dataset** so that SPARQL constraints can access all named graphs. This requires either:
- (a) Calling `ValidationEngineFactory.get().create(viewDataset, shapesGraphURI, shapesGraph, null)` directly — bypassing `ValidationUtil`, or
- (b) Subclassing `ValidationEngine` and overriding `getDataset()`

Option (a) is preferred — it is cleaner and already the pattern used in `ValidationUtil` itself (lines 66-73).

---

### Step 6 — `DatasetValidationReport.java`

A thin wrapper over the `Resource` report that adds SHACL-DS annotation helpers:

```java
public class DatasetValidationReport {
    public void annotateResults(Resource report, String shapesGraphURI, String focusGraphURI);
    public void addCombinationDefinition(Resource report, GraphCombination combination);
    public boolean conforms();
    public List<Resource> getResults();
}
```

---

### Step 7 — Meta-schema

Port `shapesDatasetShapesDataset.trig` from dotNetRDF as a resource file:

```
src/main/resources/org/topbraid/shacl/ds/shapes-dataset-meta-schema.trig
```

Load it at runtime in `ShapesDatasetEngine` for self-validation.

---

## Key Differences from dotNetRDF Implementation

| Aspect | dotNetRDF | TopBraid |
|---|---|---|
| SPARQL interception | Required (Ask.cs / Select.cs modified) | Not needed — engine already uses Dataset |
| Dataset view | Physical copy of triples | Lightweight `DatasetGraph` wrapper |
| Entry point | `ShapesDataset.Validate()` | `ShapesDatasetEngine.validate()` |
| Self-validation | `IsValid()` on `InMemoryDataset` | `ValidationUtil.validateModel()` with meta-schema |
| Graph combination materialization | Returns `IGraph` (dotNetRDF) | Returns Jena `Model` |
| Result annotation | Properties on `Result` class | RDF statements added to `sh:ValidationResult` resources |

---

## Implementation Order

1. `SHDS.java` — vocabulary (no dependencies)
2. `DatasetViewTransformer.java` — dataset view (depends on SHDS)
3. `TargetGraphSelector.java` — target resolution (depends on SHDS)
4. `GraphCombination` hierarchy — combinations (depends on SHDS)
5. `ShapesDatasetEngine.java` — orchestrator (depends on all above)
6. `DatasetValidationReport.java` — result wrapper (depends on SHDS)
7. Tests and meta-schema port

---

## Testing Plan

- `ShaclDsBasicTest.java` — normal SHACL validation works on TopBraid (smoke test)
- `DatasetViewTransformerTest.java` — view correctly sets focus graph as default
- `TargetGraphSelectorTest.java` — resolves targetGraph / Pattern / Exclude correctly
- `GraphCombinationTest.java` — Or / And / Minus produce correct models
- `ShapesDatasetEngineTest.java` — full end-to-end SHACL-DS validation
- Port the existing `test-cases/` from the SHACL-DS project
