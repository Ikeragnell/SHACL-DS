### SHACL-DS: A SHACL Extension to Validate RDF Datasets

#### Overview

SHACL-DS is an extension to SHACL (Shapes Constraint Language) designed to enable native validation of RDF Datasets, which consist of multiple named graphs. This repository presents the specifications, test cases for SHACL-DS, and a prototype implementation built as an extension of the dotNetRDF framework.

#### Features

- Shapes Dataset: Mechanism for defining SHACL shapes graphs and their targets.
- Target Declaration: Mechanism for selecting specific graphs from a dataset.
- Target Combination Declaration: Mechanism to specify combinations of graphs for validation using set operations (AND, OR, MINUS).
- SPARQL-based constraints for dataset, a specification of the behavior of SHACL’s SPARQL-based constraint applied to a dataset.
### Specification

The full specification is available in the `docs/` folder.

### Example Program

A simple example program demonstrating the usage of SHACL-DS is located in the `example/` folder.

### Installation

1. **Clone the repository:**

```
git clone https://github.com/YourUsername/SHACL-DS.git
```

2. **Initialize and update the dotNetRDF submodule:**

```
cd SHACL-DS
git submodule init
git submodule update
```

### License

This project is licensed under the MIT License.
