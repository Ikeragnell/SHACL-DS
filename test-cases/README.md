# SHACL Dataset Test CasesDS

## Note on SPARQL-based Tests

SHACL-DS engines likely rely on existing SHACL engines that may or may not support SHACL SPARQL-based constraints and/or SHACL SPARQL-based constraint components:

- **For engines compliant with SHACL SPARQL-based constraints**: Test cases 0005 and 0006 should succeed
- **For engines compliant with SHACL SPARQL-based constraint components**: Test cases 0007 should succeed

## Test Cases

- SHACL-DS-0000-SHDSDS: SHACL Dataset Shapes Dataset Shapes Dataset
- SHACL-DS-0001-TG-ALL: all data graphs as target
- SHACL-DS-0001-TG-DEFAULT: default data graphs as target
- SHACL-DS-0001-TG-IRI: data graphs with IRI as target
- SHACL-DS-0001-TG-NAMED: all named graphs as target
- SHACL-DS-0002-TGE: excluded graphs from target selection
- SHACL-DS-0003-TGC-AND: intersection of graphs as target
- SHACL-DS-0003-TGC-MINUS: difference between graphs as target
- SHACL-DS-0003-TGC-OR: union of graphs as target
- SHACL-DS-0004-TGC-COMPLEX: nested graph combinations for complex targeting
- SHACL-DS-0005-SPARQL-COMBINATION: dataset-level SPARQL-based constraints on a combination of graphs
- SHACL-DS-0005-SPARQL-DEFAULT: dataset-level SPARQL-based constraints on the default graph
- SHACL-DS-0005-SPARQL-NAMED: dataset-level SPARQL-based constraints on each named graphs
- SHACL-DS-0006-SPARQL-DATASET-VIEW: dataset-level SPARQL-based constraints dataset view transformation with access to original default graph via shds:default
- SHACL-DS-0007-SPARQL-ASK: dataset-level SPARQL-based constraint components with ASK validator
- SHACL-DS-0007-SPARQL-SELECT: dataset-level SPARQL-based constraint components with SELECT validator
- SHACL-DS-0008-TG-PATTERN: data graphs that follow a regex pattern and exclusion

