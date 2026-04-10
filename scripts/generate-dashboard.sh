#!/bin/bash
# Generates Architecture Dashboard HTML from metrics JSON
# Usage: ./scripts/generate-dashboard.sh

METRICS_FILE="/tmp/architecture-metrics.json"
HISTORY_FILE="architecture-history.json"
OUTPUT_FILE="/tmp/architecture-dashboard.html"

if [ ! -f "$METRICS_FILE" ]; then
    echo "⚠️ Metrics file not found. Run architecture tests first."
    echo "   dotnet test --filter 'ReportArchitectureMetrics'"
    exit 1
fi

# Read metrics
METRICS=$(cat "$METRICS_FILE")

# Read history for chart
HISTORY="[]"
if [ -f "$HISTORY_FILE" ]; then
    HISTORY=$(cat "$HISTORY_FILE" | jq '.history')
fi

cat > "$OUTPUT_FILE" << 'DASHBOARD_HTML'
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Architecture Health Dashboard</title>
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%);
            color: #eee;
            min-height: 100vh;
            padding: 20px;
        }
        .container { max-width: 1400px; margin: 0 auto; }
        h1 {
            text-align: center;
            font-size: 2rem;
            margin-bottom: 10px;
            background: linear-gradient(90deg, #00d4ff, #7b2cbf);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
        }
        .subtitle {
            text-align: center;
            color: #888;
            margin-bottom: 30px;
        }
        .grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
            gap: 20px;
            margin-bottom: 20px;
        }
        .card {
            background: rgba(255,255,255,0.05);
            border-radius: 16px;
            padding: 24px;
            border: 1px solid rgba(255,255,255,0.1);
        }
        .card h2 {
            font-size: 0.9rem;
            color: #888;
            text-transform: uppercase;
            letter-spacing: 1px;
            margin-bottom: 12px;
        }
        .score-container {
            text-align: center;
            padding: 20px;
        }
        .score {
            font-size: 4rem;
            font-weight: bold;
            background: linear-gradient(135deg, #00d4ff, #7b2cbf);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
        }
        .score-label { color: #888; font-size: 1.2rem; }
        .confidence {
            display: inline-block;
            padding: 8px 16px;
            border-radius: 20px;
            font-weight: bold;
            margin-top: 10px;
        }
        .confidence.high { background: rgba(0,200,100,0.2); color: #00c864; }
        .confidence.medium { background: rgba(255,200,0,0.2); color: #ffc800; }
        .confidence.low { background: rgba(255,50,50,0.2); color: #ff3232; }
        .metric-row {
            display: flex;
            justify-content: space-between;
            padding: 12px 0;
            border-bottom: 1px solid rgba(255,255,255,0.05);
        }
        .metric-row:last-child { border-bottom: none; }
        .metric-label { color: #888; }
        .metric-value { font-weight: bold; font-size: 1.1rem; }
        .metric-value.good { color: #00c864; }
        .metric-value.warning { color: #ffc800; }
        .metric-value.bad { color: #ff3232; }
        .hotspot {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 10px 12px;
            background: rgba(255,50,50,0.1);
            border-radius: 8px;
            margin-bottom: 8px;
        }
        .hotspot-name { font-weight: 500; }
        .hotspot-gaps {
            background: rgba(255,50,50,0.3);
            padding: 4px 10px;
            border-radius: 12px;
            font-size: 0.85rem;
        }
        .action {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 10px 12px;
            background: rgba(0,200,255,0.1);
            border-radius: 8px;
            margin-bottom: 8px;
        }
        .action-name { font-weight: 500; }
        .action-impact {
            background: rgba(0,200,100,0.3);
            padding: 4px 10px;
            border-radius: 12px;
            font-size: 0.85rem;
            color: #00c864;
        }
        .velocity-bar {
            height: 8px;
            background: rgba(255,255,255,0.1);
            border-radius: 4px;
            margin-top: 8px;
            overflow: hidden;
        }
        .velocity-fill {
            height: 100%;
            border-radius: 4px;
            transition: width 0.5s;
        }
        .chart-container {
            height: 200px;
            margin-top: 10px;
        }
        .timestamp {
            text-align: center;
            color: #555;
            font-size: 0.8rem;
            margin-top: 20px;
        }
    </style>
</head>
<body>
    <div class="container">
        <h1>🏗️ Architecture Health Dashboard</h1>
        <p class="subtitle">Real-time architecture governance metrics</p>

        <div class="grid">
            <!-- Score Card -->
            <div class="card score-container">
                <h2>Architecture Score</h2>
                <div class="score" id="score">--</div>
                <div class="score-label">/ 100</div>
                <div class="confidence" id="confidence">Loading...</div>
            </div>

            <!-- Velocity Card -->
            <div class="card">
                <h2>📈 Velocity</h2>
                <div class="metric-row">
                    <span class="metric-label">Current</span>
                    <span class="metric-value" id="velocity-current">--%/week</span>
                </div>
                <div class="metric-row">
                    <span class="metric-label">Required</span>
                    <span class="metric-value" id="velocity-required">--%/week</span>
                </div>
                <div class="velocity-bar">
                    <div class="velocity-fill" id="velocity-bar" style="width: 0%; background: #ff3232;"></div>
                </div>
                <div class="metric-row" style="margin-top: 12px;">
                    <span class="metric-label">Target</span>
                    <span class="metric-value" id="target-date">Q3 2026</span>
                </div>
            </div>

            <!-- Coverage Card -->
            <div class="card">
                <h2>🎯 Tenant Policy Coverage</h2>
                <div class="metric-row">
                    <span class="metric-label">Current</span>
                    <span class="metric-value" id="coverage-current">--%</span>
                </div>
                <div class="metric-row">
                    <span class="metric-label">Target</span>
                    <span class="metric-value">30%</span>
                </div>
                <div class="metric-row">
                    <span class="metric-label">Gap</span>
                    <span class="metric-value bad" id="coverage-gap">--%</span>
                </div>
            </div>
        </div>

        <div class="grid">
            <!-- Hotspots Card -->
            <div class="card">
                <h2>🔥 Debt Hotspots</h2>
                <div id="hotspots">Loading...</div>
            </div>

            <!-- Suggested Actions Card -->
            <div class="card">
                <h2>📍 Suggested Actions</h2>
                <div id="actions">Loading...</div>
            </div>

            <!-- Metrics Card -->
            <div class="card">
                <h2>📊 Key Metrics</h2>
                <div class="metric-row">
                    <span class="metric-label">Total Handlers</span>
                    <span class="metric-value" id="total-handlers">--</span>
                </div>
                <div class="metric-row">
                    <span class="metric-label">Total Requests</span>
                    <span class="metric-value" id="total-requests">--</span>
                </div>
                <div class="metric-row">
                    <span class="metric-label">Naming Violations</span>
                    <span class="metric-value" id="naming-violations">--</span>
                </div>
                <div class="metric-row">
                    <span class="metric-label">Tenant Policy Gaps</span>
                    <span class="metric-value bad" id="tenant-gaps">--</span>
                </div>
            </div>
        </div>

        <!-- Trend Chart -->
        <div class="card">
            <h2>📈 Coverage Trend</h2>
            <div class="chart-container">
                <canvas id="trendChart"></canvas>
            </div>
        </div>

        <p class="timestamp" id="timestamp">Last updated: --</p>
    </div>

    <script>
        // Metrics data (injected by script)
        const metrics = METRICS_PLACEHOLDER;
        const history = HISTORY_PLACEHOLDER;

        // Update dashboard
        function updateDashboard() {
            // Score
            document.getElementById('score').textContent = metrics.architectureScore || '--';

            // Confidence
            const confEl = document.getElementById('confidence');
            const conf = metrics.confidence || '';
            confEl.textContent = conf;
            if (conf.includes('HIGH')) confEl.className = 'confidence high';
            else if (conf.includes('MEDIUM')) confEl.className = 'confidence medium';
            else confEl.className = 'confidence low';

            // Velocity
            if (metrics.velocity) {
                document.getElementById('velocity-current').textContent = `${metrics.velocity.current.toFixed(2)}%/week`;
                document.getElementById('velocity-required').textContent = `${metrics.velocity.required.toFixed(2)}%/week`;

                const ratio = Math.min(100, (metrics.velocity.current / metrics.velocity.required) * 100);
                const bar = document.getElementById('velocity-bar');
                bar.style.width = `${ratio}%`;
                bar.style.background = ratio >= 100 ? '#00c864' : ratio >= 50 ? '#ffc800' : '#ff3232';
            }

            // Coverage
            const app = metrics.application || {};
            document.getElementById('coverage-current').textContent = `${app.tenantPolicyCoverage || 0}%`;
            document.getElementById('coverage-gap').textContent = `${Math.max(0, 30 - (app.tenantPolicyCoverage || 0))}%`;

            // Key metrics
            document.getElementById('total-handlers').textContent = app.totalHandlers || '--';
            document.getElementById('total-requests').textContent = app.totalRequests || '--';
            document.getElementById('naming-violations').textContent = app.namingViolations || '--';
            document.getElementById('tenant-gaps').textContent = app.tenantPolicyGaps || '--';

            // Hotspots
            const hotspotsEl = document.getElementById('hotspots');
            const hotspots = metrics.hotspots || {};
            hotspotsEl.innerHTML = Object.entries(hotspots)
                .map(([name, gaps]) => `
                    <div class="hotspot">
                        <span class="hotspot-name">${name}</span>
                        <span class="hotspot-gaps">${gaps} gaps</span>
                    </div>
                `).join('') || '<p style="color:#888">No hotspots</p>';

            // Actions
            const actionsEl = document.getElementById('actions');
            const actions = metrics.suggestedActions || [];
            actionsEl.innerHTML = actions
                .map(a => `
                    <div class="action">
                        <span class="action-name">${a.Name || a}</span>
                        <span class="action-impact">+${(a.Impact || 0.8).toFixed(1)}%</span>
                    </div>
                `).join('') || '<p style="color:#888">No actions needed</p>';

            // Timestamp
            document.getElementById('timestamp').textContent = `Last updated: ${new Date(metrics.timestamp).toLocaleString()}`;

            // Trend chart
            if (history.length > 0) {
                const ctx = document.getElementById('trendChart').getContext('2d');
                new Chart(ctx, {
                    type: 'line',
                    data: {
                        labels: history.map(h => h.date),
                        datasets: [{
                            label: 'Coverage %',
                            data: history.map(h => h.metrics.tenantPolicyCoverage),
                            borderColor: '#00d4ff',
                            backgroundColor: 'rgba(0,212,255,0.1)',
                            fill: true,
                            tension: 0.3
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: { legend: { display: false } },
                        scales: {
                            y: { beginAtZero: true, max: 100, grid: { color: 'rgba(255,255,255,0.05)' } },
                            x: { grid: { color: 'rgba(255,255,255,0.05)' } }
                        }
                    }
                });
            }
        }

        updateDashboard();
    </script>
</body>
</html>
DASHBOARD_HTML

# Inject actual data
sed -i '' "s/METRICS_PLACEHOLDER/$( echo "$METRICS" | jq -c . | sed 's/[&/\]/\\&/g' )/g" "$OUTPUT_FILE"
sed -i '' "s/HISTORY_PLACEHOLDER/$( echo "$HISTORY" | jq -c . | sed 's/[&/\]/\\&/g' )/g" "$OUTPUT_FILE"

echo "✅ Dashboard generated: $OUTPUT_FILE"
echo ""
echo "Open in browser:"
echo "  open $OUTPUT_FILE"
