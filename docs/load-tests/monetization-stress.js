import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    scenarios: {
        // Scenario 1: 500 concurrent Invoices (Stress Plan Enforcement)
        invoice_stress: {
            executor: 'constant-vus',
            vus: 50,
            duration: '30s',
            gracefulStop: '5s',
            exec: 'invoice_creation',
        },
        // Scenario 2: 200 concurrent Upgrades
        upgrade_stress: {
            executor: 'per-vu-iterations',
            vus: 20,
            iterations: 10,
            maxDuration: '1m',
            exec: 'subscription_upgrade',
        },
        // Scenario 3: Mixed Traffic
        mixed_traffic: {
            executor: 'ramping-vus',
            startVUs: 0,
            stages: [
                { duration: '30s', target: 50 }, // Ramp up
                { duration: '1m', target: 50 },  // Steady load
                { duration: '20s', target: 0 },  // Ramp down
            ],
            exec: 'mixed_load',
        },
    },
    thresholds: {
        http_req_failed: ['rate<0.01'], // http errors should be less than 1%
        http_req_duration: ['p(95)<500'], // 95% of requests should be below 500ms
    },
};

const API_BASE_URL = __ENV.API_URL || 'http://localhost:8080/api/v1';
const TENANT_ID = '3fa85f64-5717-4562-b3fc-2c963f66afa6'; // Replace with a valid seed tenant
const AUTH_TOKEN = __ENV.AUTH_TOKEN;

const params = {
    headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${AUTH_TOKEN}`,
        'X-Tenant-Id': TENANT_ID,
    },
};

export function invoice_creation() {
    const payload = JSON.stringify({
        customerId: '4fa85f64-5717-4562-b3fc-2c963f66afa6',
        amount: 100.0,
        currency: 'USD',
        dueDate: '2026-12-31T00:00:00Z',
        items: [{ description: 'Test Item', quantity: 1, unitPrice: 100.0 }]
    });

    const res = http.post(`${API_BASE_URL}/invoices`, payload, params);
    
    check(res, {
        'is status 201': (r) => r.status === 201,
        'is status 403': (r) => r.status === 403, // Plan limit reached is acceptable under stress
    });
}

export function subscription_upgrade() {
    const payload = JSON.stringify({
        newPlanId: '9fa85f64-5717-4562-b3fc-2c963f66afa6' // Enterprise Plan
    });

    const res = http.post(`${API_BASE_URL}/tenants/${TENANT_ID}/subscription/upgrade`, payload, params);
    
    check(res, {
        'is status 200': (r) => r.status === 200,
    });
}

export function mixed_load() {
    // Randomly choose between Read and Write
    if (Math.random() < 0.7) {
        // Read Dashboard
        const res = http.get(`${API_BASE_URL}/tenants/${TENANT_ID}/subscription`, params);
        check(res, { 'is status 200': (r) => r.status === 200 });
    } else {
        // Write Invoice
        invoice_creation();
    }
    sleep(1);
}
