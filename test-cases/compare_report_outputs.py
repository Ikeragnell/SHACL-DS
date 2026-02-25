#!/usr/bin/env python3
"""
Compare SHACL-DS test reports by validation result content.
Ignores sh:resultMessage (text varies by engine) and blank node identity.
"""

import os
import sys
import argparse
from pathlib import Path
from datetime import datetime
from rdflib import Graph, URIRef, Literal, BNode, Namespace, RDF
from collections import defaultdict

SH = Namespace("http://www.w3.org/ns/shacl#")
SHDS = Namespace("https://w3id.org/shacl-ds#")


class TestResult:
    def __init__(self, test_dir, file1, file2, is_equivalent, triples1=0, triples2=0, 
                 results1=0, results2=0, error=None):
        self.test_dir = test_dir
        self.file1 = file1
        self.file2 = file2
        self.is_equivalent = is_equivalent
        self.triples1 = triples1
        self.triples2 = triples2
        self.results1 = results1
        self.results2 = results2
        self.error = error
    
    @property
    def status(self):
        if self.error:
            return "ERROR"
        return "PASS" if self.is_equivalent else "FAIL"


def load_graph_safe(filepath):
    """Load RDF file with format detection."""
    g = Graph()
    try:
        suffix = Path(filepath).suffix.lower()
        format_map = {'.ttl': 'turtle', '.rdf': 'xml', '.nt': 'ntriples', '.n3': 'n3'}
        fmt = format_map.get(suffix, 'turtle')
        g.parse(filepath, format=fmt)
        return g, None
    except Exception as e:
        return None, str(e)


def get_validation_result_fingerprint(result_node, g):
    """
    Create a canonical string representation of a validation result.
    Ignores sh:resultMessage (text varies by engine) and treats all blank nodes as equivalent.
    """
    props = {}
    
    # Properties to ignore (engine-specific text)
    IGNORE_PROPS = {
        str(SH.resultMessage),  # Different engines generate different text
    }
    
    for p, o in g.predicate_objects(result_node):
        p_str = str(p)
        
        # Skip ignored properties
        if p_str in IGNORE_PROPS:
            continue
            
        # Handle blank nodes as '[]' regardless of identity
        if isinstance(o, BNode):
            o_val = "[]"
        elif isinstance(o, URIRef):
            o_val = str(o)
        elif isinstance(o, Literal):
            o_val = str(o)
        else:
            o_val = str(o)
            
        props[p_str] = o_val
    
    return "|".join(f"{k}={v}" for k, v in sorted(props.items()))


def compare_validation_reports(file1, file2):
    """Compare two SHACL validation reports by result content."""
    g1, err1 = load_graph_safe(file1)
    if err1:
        return None, f"Failed to load {file1}: {err1}"
        
    g2, err2 = load_graph_safe(file2)
    if err2:
        return None, f"Failed to load {file2}: {err2}"

    # Extract validation results from both graphs
    def extract_results(g):
        results = set()
        for report in g.subjects(RDF.type, SH.ValidationReport):
            for result in g.objects(report, SH.result):
                fingerprint = get_validation_result_fingerprint(result, g)
                results.add(fingerprint)
        return results

    results1 = extract_results(g1)
    results2 = extract_results(g2)
    
    # Check equivalence
    is_equivalent = results1 == results2
    
    return TestResult(
        test_dir=Path(file1).parent.name,
        file1=file1,
        file2=file2,
        is_equivalent=is_equivalent,
        triples1=len(g1),
        triples2=len(g2),
        results1=len(results1),
        results2=len(results2)
    ), None


def run_comparison(base_dir, verbose=False):
    """Run comparison across all test case directories."""
    base_path = Path(base_dir)
    results = []
    
    if not base_path.exists():
        print(f"❌ Directory not found: {base_dir}")
        return results

    test_dirs = [d for d in base_path.iterdir() if d.is_dir()]
    
    for test_dir in sorted(test_dirs):
        report1 = test_dir / "report.ttl"
        report2 = test_dir / "report_topbraid.ttl"
        
        if report1.exists() and report2.exists():
            if verbose:
                print(f"🔍 Checking {test_dir.name}...")
                
            result, error = compare_validation_reports(str(report1), str(report2))
            if error:
                result = TestResult(
                    test_dir=test_dir.name,
                    file1=str(report1),
                    file2=str(report2),
                    is_equivalent=False,
                    error=error
                )
            
            results.append(result)
            
            if verbose:
                status_emoji = "✅" if result.status == "PASS" else "❌" if result.status == "FAIL" else "⚠️"
                print(f"   {status_emoji} {result.status}")
        elif verbose:
            print(f"⏭️  Skipping {test_dir.name} (missing reports)")

    return results


def generate_report(results, output_file=None):
    """Generate and print summary report."""
    passed = sum(1 for r in results if r.status == "PASS")
    failed = sum(1 for r in results if r.status == "FAIL")
    errors = sum(1 for r in results if r.status == "ERROR")
    total = len(results)

    lines = []
    lines.append("=" * 80)
    lines.append("SHACL-DS Validation Report Comparison Summary")
    lines.append("=" * 80)
    lines.append(f"Total tests:    {total}")
    lines.append(f"Passed:         {passed} ✅")
    lines.append(f"Failed:         {failed} ❌")
    lines.append(f"Errors:         {errors} ⚠️")
    lines.append(f"Success rate:   {passed/total*100:.1f}%" if total > 0 else "N/A")
    lines.append("=" * 80)

    if failed > 0:
        lines.append("\nFailed tests (different validation results):")
        for r in results:
            if r.status == "FAIL":
                lines.append(f"  - {r.test_dir}: {r.results1} vs {r.results2} results")

    if errors > 0:
        lines.append("\nTests with errors:")
        for r in results:
            if r.status == "ERROR":
                lines.append(f"  - {r.test_dir}: {r.error}")

    report_text = "\n".join(lines)
    print(report_text)

    if output_file:
        with open(output_file, 'w', encoding='utf-8') as f:
            f.write(f"SHACL-DS Test Comparison Report\n")
            f.write(f"Generated: {datetime.now().isoformat()}\n")
            f.write(report_text)
        print(f"\n📄 Report saved: {output_file}")

    return failed == 0 and errors == 0


def main():
    parser = argparse.ArgumentParser(
        description="Compare SHACL validation reports (ignores message text and blank node IDs)"
    )
    parser.add_argument("--dir", "-d", default=r"D:\Doctorat\SHACL-DS\test-cases",
                       help="Base directory containing test case folders")
    parser.add_argument("--verbose", "-v", action="store_true",
                       help="Show progress for each test")
    parser.add_argument("--report", "-r", default="comparison_summary.txt",
                       help="Output file for summary report")

    args = parser.parse_args()

    results = run_comparison(args.dir, verbose=args.verbose)
    
    if results:
        all_ok = generate_report(results, args.report)
        sys.exit(0 if all_ok else 1)
    else:
        print("No tests found to run.")
        sys.exit(2)


if __name__ == "__main__":
    main()
