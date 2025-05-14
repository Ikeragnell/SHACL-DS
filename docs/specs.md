# SHACL-DS Specification (DRAFT)

## Overview

**SHACL-DS** is an extension to [SHACL](https://www.w3.org/TR/shacl/) that enables native validation of RDF datasets. It introduces the following features:

- **SHACL dataset**: An RDF dataset that contains a set of SHACL shapes graphs, and their targets, which are declaratively specified.
- **TargetGraph**: A mechanism for selecting specific graphs from a dataset that a shapes graph targets.
- **Target Combination Declaration**: A mechanism to specify a combination of graphs that a shapes graph targets.
- **SPARQL-based constraints and SPARQL-based constraint components for dataset**: A specification of the behavior of SHACL's SPARQL-based constraint applied to a dataset.

## Document Convention

This specification uses the following prefix bindings throughout:

| Prefix | Namespace |
|--------|-----------|
| `sh`   | `http://www.w3.org/ns/shacl#` |
| `shds` | `http://www.w3.org/ns/shacl-dataset#` |
| `foaf` | `http://xmlns.com/foaf/0.1/` |
| `ex`   | `http://example.org/` |

All examples use RDF serializations in [TriG](https://www.w3.org/TR/trig/) format with these prefixes.

## Predefined IRIs

SHACL-DS defines the following predefined IRIs for use in target declarations and graph combinations:

- `shds:default` — Refers to the default graph of the data dataset.
- `shds:named` — Refers to all named graphs in the data dataset.
- `shds:all` — Refers to all graphs in the data dataset, including both the default and named graphs.

## Shapes Dataset

A **Shapes Dataset** is an RDF dataset that contains one or more shapes graphs. These graphs may declare which data graphs they target using either `shds:targetGraph` or `shds:targetGraphCombination`. Shapes graphs with no declared target are skipped during validation.

Declaration of a shapes graph’s targets can be done in one of two ways:

- In the **default graph** of the Shapes Dataset (centralized declaration).
- In the **named graph** corresponding to the shapes graph itself (decentralized declaration).


## Target Graph Declaration

Use the `shds:targetGraph` predicate to declare that a shapes graph targets specific named graphs in the data dataset.

#### Example

The following example declares that the shapes graph `ex:shapesGraph1` targets all named graphs in the data dataset. The shape `ex:AliceIsPersonShape` specifies that `ex:Alice` must be an instance of `foaf:Person` in each targeted graph.

```trig
ex:shapesGraph1 shds:targetGraph shds:named.
ex:shapesGraph1 {
    ex:AliceIsPersonShape
        sh:targetNode ex:Alice ;    
        sh:class ex:Person.
}
```

### Exclusions

The `shds:targetGraphExclude` predicate allows specific graphs to be excluded from the set of target graphs. Exclusions are applied after inclusions.

#### Example

The following example declares that the shapes graph `ex:shapesGraph1` targets all named graphs in the data dataset that are neither `ex:datagraph1` nor `ex:datagraph1`. The shape `ex:AliceIsPersonShape` specifies that `ex:Alice` must be an instance of `foaf:Person` in each targeted graph.

```trig
ex:shapesGraph1 
    shds:targetGraph shds:named; 
    shds:targetGraphExclude ex:datagraph1; 
    shds:targetGraphExclude ex:datagraph2.
ex:shapesGraph1 {
    ex:AliceIsPersonShape
        sh:targetNode ex:Alice ;    
        sh:class ex:Person .
}
```
## Target Graph Combination

In addition to selecting existing graphs, SHACL-DS supports the definition of **target graph combinations** using set operations. This allows a shapes graph to validate a new graph constructed from multiple graphs.

Use the `shds:targetGraphCombination` predicate to declare such combinations. Supported operators are:

- `shds:or` — union of multiple graphs
- `shds:and` — intersection of multiple graphs
- `shds:minus` — difference between two graphs (i.e., subtracting one from another)

These operators accept RDF lists of graph IRIs or other graph combinations. The predefined IRIs `shds:default`, `shds:named`, and `shds:all` can also be used as operands, serving as syntactic sugar for a list of matching graph IRIs.

#### Example

The following example declares that `ex:shapesGraph1` targets the union of all graphs minus the  `ex:datagraph1` named graph. For this graph combination, every person must know someone.

```trig
ex:shapeGraph1 shds:targetGraphCombination [shds:minus ([shds:union (shds:all)] ex:dataGraph1);].
ex:shapeGraph1 {
ex:PersonKnowsSomeone
    sh:targetClass foaf:Person ;    
    sh:property [                 
        sh:path foaf:knows ;
        sh:minCount 1 ; 
    ] ;
}
```

## SPARQL-based Constraints for Dataset

SHACL-DS formalizes how SPARQL queries in SPARQL-based constraints and SPARQL-based constraint components should be executed when validating RDF datasets. This ensures consistent and reusable constraint behavior across target graphs and graph combinations.

### 1. Validation Context

SPARQL queries are executed over the **data dataset** only. We strongly sugget that the shapes graph SHOULD NOT be included in the query dataset during execution to prevent unintended behavior.

SHACL-DS does not define the behavior of the pre-bound variables `$shapesGraph` and `$currentShape` because these are optional and not interoperable across SHACL-SPARQL engines. Their usage is deferred to future work.

### 2. Dataset View Transformation

To ensure **reusability** of constraints across focus graphs (whether named, default, or combinations), SHACL-DS applies a **dataset view transformation** before executing a SPARQL query:

- **Default Graph Target**  
  No transformation is performed. The dataset is used as-is.

- **Named Graph Target**  
  The named graph becomes the default graph. The original default graph is renamed to `shds:default`.

- **Combination Target**  
  The combined graph is treated as the default graph. The original default graph is preserved and renamed to `shds:default`.

These transformations ensure that all SPARQL queries are written against the default graph context while still having access to the rest of the dataset structure through named graphs.

### Example

The following example illustrates a SPARQL-based constraint that checks whether each person knows at least one person from the `ex:goodPersonGraph`. The shapes graph targets all graphs **except** that one.

```trig
ex:shapeGraph1 
    shds:targetGraph shds:all ;
    shds:targetGraphExclude ex:goodPersonGraph .

ex:shapeGraph1 {
    ex:knowsGoodPersonShape
        sh:targetClass foaf:Person ;
        sh:sparql [
            sh:select """
                PREFIX foaf: <http://xmlns.com/foaf/0.1/>

                SELECT DISTINCT $this
                WHERE {
                    $this a foaf:Person .
                    FILTER NOT EXISTS {
                        GRAPH ex:goodPersonGraph { ?goodPerson a foaf:Person . }
                        $this foaf:knows ?goodPerson .
                    }
                }
            """
        ] .
}
```

## Validation Report

SHACL-DS extends the SHACL validation reporting mechanism with two additional predicates that provide dataset-level context:

- `shds:sourceShapeGraph`: Identifies the shapes graph in the Shapes Dataset that defined the shape responsible for the result.
- `shds:focusGraph`: Identifies the data graph (or graph combination) from the Data Dataset where the focus node originated.

These additions help disambiguate shape usage and trace validation results to both the shapes and data graphs involved in the process.

#### Example

The following example demonstrates SHACL-DS validation over a dataset with two named graphs. The shapes graphs `ex:shapeGraphSingleTarget1` and `ex:shapeGraphSingleTarget2` each target one of the data graphs. The shape requires that all `foaf:Person` nodes have at least one `foaf:knows` property.

##### Data Dataset

```trig
@prefix ex: <http://example.org/> .
@prefix shds: <http://www.w3.org/ns/shacl-dataset#>.
@prefix sh: <http://www.w3.org/ns/shacl#>.
@prefix foaf: <http://xmlns.com/foaf/0.1/> .

ex:Alice a foaf:Person.
ex:Alice foaf:knows ex:Zach.

ex:Bob a foaf:Person.

ex:dataGraph1 {
    ex:Charlie a foaf:Person.
    ex:Charlie foaf:knows ex:Zach.

    ex:David a foaf:Person.
}

ex:dataGraph2 {
    ex:Eve a foaf:Person.
    ex:Eve foaf:knows ex:Zach.
}
```
##### Shapes Dataset
```trig
@prefix ex: <http://example.org/> .
@prefix shds: <http://www.w3.org/ns/shacl-dataset#>.
@prefix sh: <http://www.w3.org/ns/shacl#>.
@prefix foaf: <http://xmlns.com/foaf/0.1/> .

ex:shapeGraphSingleTarget1 shds:targetGraph ex:dataGraph1.

ex:shapeGraphSingleTarget1 {
    ex:PersonShape
        a sh:NodeShape ;
        sh:targetClass foaf:Person ;    
        sh:property [                 
            sh:path foaf:knows ;
            sh:minCount 1 ;
        ] ;
}

ex:shapeGraphSingleTarget2 {
    ex:shapeGraphSingleTarget2 shds:targetGraph ex:dataGraph2.

    ex:PersonShape
        a sh:NodeShape ;
        sh:targetClass foaf:Person ;    
        sh:property [                 
            sh:path foaf:knows ;
            sh:minCount 1 ;
        ] ;
}
```
##### Validation report (Turtle)

```turtle
@prefix rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#> .
@prefix sh: <http://www.w3.org/ns/shacl#> .
@prefix shds: <http://www.w3.org/ns/shacl-dataset#> .
@prefix xsd: <http://www.w3.org/2001/XMLSchema#> .
@prefix ex: <http://example.org/> .

[ a sh:ValidationReport ;
    sh:conforms false ;
    sh:result [
        a sh:ValidationResult ;
        sh:resultSeverity sh:Violation ;
        sh:focusNode ex:David ;
        sh:resultMessage "There should be at least 1 value(s)." ;
        sh:resultPath foaf:knows ;
        sh:sourceConstraintComponent sh:MinCountConstraintComponent ;
        sh:sourceShape ex:PersonShape ;
        shds:sourceShapeGraph ex:shapeGraphSingleTarget1 ;
        shds:focusGraph ex:dataGraph1
    ] .
]
```

## Test Cases

SHACL-DS includes a comprehensive suite of [test cases](../test-cases) designed to validate the correctness, expressiveness, and interoperability of the SHACL-DS specification and its implementation. These test cases serve both as validation assets and as executable examples to help implementers and users understand how SHACL-DS works in practice.

Each test case includes:

- A **Data Dataset** in TriG format.
- A **Shapes Dataset** in TriG format.
- An **expected validation report** in Turtle showing the expected output.

The test suite is structured to cover all  SHACL-DS features, from basic to advanced scenarios.
