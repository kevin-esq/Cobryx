#!/bin/bash
# Updates architecture-history.json with current metrics
# Run after successful CI build to track trends
# Usage: ./scripts/update-architecture-history.sh

set -e

METRICS_FILE="/tmp/architecture-metrics.json"
HISTORY_FILE="architecture-history.json"

if [ ! -f "$METRICS_FILE" ]; then
    echo "⚠️ Metrics file not found. Run architecture tests first."
    exit 1
fi

# Get current date and commit
DATE=$(date +%Y-%m-%d)
COMMIT=$(git rev-parse --short HEAD 2>/dev/null || echo "unknown")

# Extract metrics
if command -v jq &> /dev/null; then
    HANDLERS=$(jq '.application.totalHandlers' "$METRICS_FILE")
    REQUESTS=$(jq '.application.totalRequests' "$METRICS_FILE")
    COVERAGE=$(jq '.application.tenantPolicyCoverage' "$METRICS_FILE")
    NAMING=$(jq '.application.namingViolations' "$METRICS_FILE")
    GAPS=$(jq '.application.tenantPolicyGaps' "$METRICS_FILE")
    
    # Create new entry
    NEW_ENTRY=$(cat <<EOF
{
  "date": "$DATE",
  "commit": "$COMMIT",
  "metrics": {
    "totalHandlers": $HANDLERS,
    "totalRequests": $REQUESTS,
    "tenantPolicyCoverage": $COVERAGE,
    "namingViolations": $NAMING,
    "tenantPolicyGaps": $GAPS
  }
}
EOF
)
    
    # Append to history (keep last 100 entries)
    jq --argjson entry "$NEW_ENTRY" '.history = (.history + [$entry] | .[-100:])' "$HISTORY_FILE" > "${HISTORY_FILE}.tmp"
    mv "${HISTORY_FILE}.tmp" "$HISTORY_FILE"
    
    echo "✅ Architecture history updated"
    echo "   Date: $DATE"
    echo "   Commit: $COMMIT"
    echo "   Coverage: $COVERAGE%"
else
    echo "⚠️ jq not installed. Cannot update history automatically."
    exit 1
fi
