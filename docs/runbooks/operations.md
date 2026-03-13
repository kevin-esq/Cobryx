# Sample Runbooks (Skeleton)

Recovery checklists for production incidents.

## [RB-001] Outbox Worker Stalled
1. Check Redpanda connectivity.
2. Verify `idx_outbox_unpublished` index health.
3. Check `RetryCount` thresholds in `FinancialOutboxEvent`.
4. Scale worker pods if lag > 10,000 events.

## [RB-002] Shadow Replay Drift Detected
1. Identify the drifting `TenantId` and `LoanId`.
2. Compare `ShadowBalance` vs `LedgerBalance` using the sequence ID.
3. Replay specific sequence ranges via `ShadowReplayEngine.RebuildAsync`.

## [RB-003] Disaster Recovery (Ledger Reconstruction)
1. Restore core `JournalEntries` from backup.
2. Re-trigger SRE rebuild from sequence 0.
3. Warm Redis cache from SRE finalized state.
