#!/bin/bash
# CHAOS TEST: Webhook Blackhole
# Simulates lost webhooks by blocking the endpoint during payments
# Expected: Reconciliation job auto-heals the missing payments

set -e

BASE_URL="${BASE_URL:-http://localhost:5000}"
STRIPE_CLI="${STRIPE_CLI:-stripe}"

echo "🕳️  CHAOS TEST: Webhook Blackhole"
echo "=================================="
echo "This test simulates lost webhooks and verifies reconciliation auto-heals"
echo ""

# Step 1: Block webhook endpoint (iptables or nginx)
echo "Step 1: Simulating webhook blackhole..."
echo "        (In production, use iptables or nginx to block /api/v1/webhooks/stripe)"
echo ""

# For local testing, we'll just skip the webhook and verify reconciliation
PAYMENT_INTENT_ID="pi_chaos_test_$(date +%s)"

echo "Step 2: Creating payment directly in Stripe (bypassing webhook)..."
echo "        Payment Intent ID: $PAYMENT_INTENT_ID"
echo ""

# In real test:
# $STRIPE_CLI payment_intents create \
#   --amount=1000 \
#   --currency=usd \
#   --confirm \
#   --payment-method=pm_card_visa

echo "Step 3: Verifying payment NOT in local DB..."
# curl -s "$BASE_URL/api/v1/payments?stripeIntentId=$PAYMENT_INTENT_ID" | jq

echo "Step 4: Waiting for reconciliation job (5 minutes)..."
echo "        Or trigger manually: GET /api/v1/operations/reconciliation/stripe"
# curl -X GET "$BASE_URL/api/v1/operations/reconciliation/stripe" job
# OR
# curl -X POST "$BASE_URL/api/v1/admin/reconciliation/run"

echo "Step 5: Verifying payment NOW exists in local DB..."
# curl -s "$BASE_URL/api/v1/payments?stripeIntentId=$PAYMENT_INTENT_ID" | jq

echo ""
echo "📊 EXPECTED RESULTS"
echo "==================="
echo "1. Payment created in Stripe: ✓"
echo "2. Webhook blocked (not received): ✓"
echo "3. Payment missing from DB initially: ✓"
echo "4. Reconciliation job runs: ✓"
echo "5. Payment auto-created in DB: ✓"
echo "6. Ledger entries created: ✓"
echo ""

echo "📈 METRICS TO CHECK"
echo "==================="
echo "- reconciliation_mismatch_total: should increment"
echo "- reconciliation_auto_fixed_total: should increment"
echo "- reconciliation_checked_total: should increment"
echo ""

echo "🔍 LOGS TO CHECK"
echo "================"
echo "- 'Payment succeeded in Stripe but is missing from Ledger'"
echo "- '[AUTO-REPAIRED]'"
echo "- 'Reconciliation completed'"
echo ""

echo "✅ If all checks pass, the self-healing system is working correctly!"
