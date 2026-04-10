#!/bin/bash
# Architecture CI Gate
# Fails if architecture metrics regress from baseline
# Usage: ./scripts/architecture-gate.sh

set -e

echo "╔══════════════════════════════════════════╗"
echo "║       ARCHITECTURE CI GATE               ║"
echo "╚══════════════════════════════════════════╝"

# Run architecture tests and capture output
echo "Running architecture tests..."
dotnet test Cobryx.Architecture.Tests/Cobryx.Architecture.Tests.csproj --configuration Release --no-build -v q 2>&1 || {
    echo "❌ Architecture tests failed!"
    exit 1
}

# Check if metrics file was generated
METRICS_FILE="/tmp/architecture-metrics.json"
if [ ! -f "$METRICS_FILE" ]; then
    echo "⚠️ Metrics file not found at $METRICS_FILE"
    echo "✅ Architecture tests passed (no metrics to validate)"
    exit 0
fi

# Load baseline
BASELINE_FILE="architecture-baseline.json"
if [ ! -f "$BASELINE_FILE" ]; then
    echo "⚠️ Baseline file not found: $BASELINE_FILE"
    exit 1
fi

# Extract values using jq (or python as fallback)
if command -v jq &> /dev/null; then
    CURRENT_SCORE=$(jq '.architectureScore' "$METRICS_FILE")
    CURRENT_NAMING=$(jq '.application.namingViolations' "$METRICS_FILE")
    CURRENT_TENANT=$(jq '.application.tenantPolicyGaps' "$METRICS_FILE")
    BASELINE_SCORE=$(jq '.baselines.minimumArchitectureScore // 20' "$BASELINE_FILE")
    BASELINE_NAMING=$(jq '.baselines.namingViolations' "$BASELINE_FILE")
    BASELINE_TENANT=$(jq '.baselines.tenantPolicyGaps' "$BASELINE_FILE")

    # Get hotspots
    echo ""
    echo "🔥 DEBT HOTSPOTS:"
    jq -r '.hotspots | to_entries | .[] | "   - \(.key): \(.value) gaps"' "$METRICS_FILE" 2>/dev/null || echo "   (none)"
else
    # Python fallback
    CURRENT_SCORE=$(python3 -c "import json; print(json.load(open('$METRICS_FILE')).get('architectureScore', 0))")
    CURRENT_NAMING=$(python3 -c "import json; print(json.load(open('$METRICS_FILE'))['application']['namingViolations'])")
    CURRENT_TENANT=$(python3 -c "import json; print(json.load(open('$METRICS_FILE'))['application']['tenantPolicyGaps'])")
    BASELINE_SCORE=$(python3 -c "import json; print(json.load(open('$BASELINE_FILE'))['baselines'].get('minimumArchitectureScore', 20))")
    BASELINE_NAMING=$(python3 -c "import json; print(json.load(open('$BASELINE_FILE'))['baselines']['namingViolations'])")
    BASELINE_TENANT=$(python3 -c "import json; print(json.load(open('$BASELINE_FILE'))['baselines']['tenantPolicyGaps'])")
fi

echo ""
echo "📊 Current Metrics:"
echo "   🏆 Architecture Score: $CURRENT_SCORE/100 (minimum: $BASELINE_SCORE)"
echo "   Naming Violations: $CURRENT_NAMING (baseline: $BASELINE_NAMING)"
echo "   Tenant Policy Gaps: $CURRENT_TENANT (baseline: $BASELINE_TENANT)"
echo ""

# Check for regressions
FAILED=0

if [ "$CURRENT_SCORE" -lt "$BASELINE_SCORE" ]; then
    echo "❌ FAIL: Architecture score below minimum ($CURRENT_SCORE < $BASELINE_SCORE)"
    FAILED=1
fi

if [ "$CURRENT_NAMING" -gt "$BASELINE_NAMING" ]; then
    echo "❌ FAIL: Naming violations increased ($CURRENT_NAMING > $BASELINE_NAMING)"
    FAILED=1
fi

if [ "$CURRENT_TENANT" -gt "$BASELINE_TENANT" ]; then
    echo "❌ FAIL: Tenant policy gaps increased ($CURRENT_TENANT > $BASELINE_TENANT)"
    FAILED=1
fi

# Check if baseline was updated without justification
if git diff --name-only origin/main...HEAD 2>/dev/null | grep -q "architecture-baseline.json"; then
    echo "⚠️  Architecture baseline update detected."
    
    # Check current branch history vs origin/main for the tag
    if ! git log origin/main..HEAD --pretty=%B | grep -q "\[ARCH_BASELINE\]"; then
        echo "❌ FAIL: Baseline updated without [ARCH_BASELINE] tag in the PR commit history."
        echo "   If intentionally updating baseline, include [ARCH_BASELINE] in at least one commit message."
        FAILED=1
    else
        echo "✅ [ARCH_BASELINE] tag found in PR history."
    fi
fi

if [ "$FAILED" -eq 1 ]; then
    echo ""
    echo "╔══════════════════════════════════════════╗"
    echo "║  ❌ ARCHITECTURE GATE FAILED             ║"
    echo "╚══════════════════════════════════════════╝"
    exit 1
fi

echo "╔══════════════════════════════════════════╗"
echo "║  ✅ ARCHITECTURE GATE PASSED             ║"
echo "╚══════════════════════════════════════════╝"
exit 0
