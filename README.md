### SHACL-DS: A SHACL Extension to Validate RDF Datasets

#### Overview

SHACL-DS is an extension to SHACL (Shapes Constraint Language) designed to enable native validation of RDF Datasets, which consist of multiple named graphs. This repository presents the specifications, test cases for SHACL-DS, and a prototype implementation built as an extension of the dotNetRDF framework.

#### Features

- Shapes Dataset: Mechanism for defining SHACL shapes graphs and their targets.
- Target Declaration: Mechanism for selecting specific graphs from a dataset.
- Target Combination Declaration: Mechanism to specify combinations of graphs for validation using set operations (AND, OR, MINUS).
- SPARQL-based constraints for dataset, a specification of the behavior of SHACL’s SPARQL-based constraint applied to a dataset.
  
### Specification

TODO: The full specification is available in the `docs/` folder.

### Installation

0. **Prerequisite**

- **[.NET SDK](https://dotnet.microsoft.com/download)** (Version 8 recommended)

1. **Clone the repository:**

```
git clone https://github.com/Ikeragnell/SHACL-DS.git
cd SHACL-DS
```
2. **Build the CLI**

Ensure the CLI project targets your SDK version (net8.0) in `src/SHACL-DS.cli/SHACL-DS.cli.csproj`:

```xml
   <TargetFramework>net8.0</TargetFramework>
```

```bash
dotnet build src/SHACL-DS.cli/SHACL-DS.cli.csproj
```

### Usage

```bash
dotnet run --project src/SHACL-DS.cli --data <path> --shapes <path> [--report <path>] [--output-format ttl|json] [--mode auto|shacl|shaclds]
```

Example:

```bash
dotnet run --project src/SHACL-DS.cli --data test-cases/SHACL-DS-0001-TG-ALL/data.trig --shapes test-cases/SHACL-DS-0001-TG-ALL/shapes.trig 
```


### Programmatic example

A simple .NET example program demonstrating the usage of SHACL-DS is located in the `example/` folder.

### Use Cases

The `use-cases/` folder contains runnable and more realistic scenarios that demonstrate why dataset-aware validation matters.

### License

This project is licensed under the MIT License.
