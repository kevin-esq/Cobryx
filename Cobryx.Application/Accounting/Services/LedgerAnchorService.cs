using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Accounting.Interfaces;
using Cobryx.Domain.Accounting.Models;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Accounting.Services
{
    public sealed class LedgerAnchorService(
        ILedgerAnchorStore anchorStore,
        ILedgerSigner signer,
        ICobryxDbContext context,
        IClock clock) : ILedgerAnchorService
    {
        private readonly ILedgerAnchorStore _anchorStore = anchorStore;
        private readonly ILedgerSigner _signer = signer;
        private readonly ICobryxDbContext _context = context;
        private readonly IClock _clock = clock;

        public async Task TriggerAnchoringIfRequiredAsync(
            Guid tenantId,
            string currentHash,
            long sequence,
            decimal amount,
            object request,
            CancellationToken ct = default)
        {
            // 1. Fetch Tenant Settings for Quantitative Threshold
            var tenant = await _context.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

            var threshold = tenant?.Settings?.HighRiskLedgerThreshold ?? 10000m;

            // 2. Evaluate Hybrid Risk Matrix
            var isHighRisk =
                request is IHighRiskOperation ||
                amount >= threshold;

            var isPeriodic = sequence % 50 == 0;

            if (isHighRisk || isPeriodic)
            {
                await PerformAnchoringAsync(tenantId, currentHash, sequence, ct);
            }
        }

        private async Task PerformAnchoringAsync(Guid tenantId, string currentHash, long sequence, CancellationToken ct)
        {
            // 3. Fetch link to the Previous Anchor for chaining
            var latestAnchor = _anchorStore.GetLatest(tenantId, ct);
            var prevAnchorHash = latestAnchor?.Signature ?? "genesis";

            // 4. Build Anchor
            var anchor = new LedgerAnchor(
                tenantId,
                sequence,
                currentHash,
                prevAnchorHash,
                _clock.UtcNow,
                string.Empty // Signature set in next step
            );

            // 5. Sign Anchor
            var signature = _signer.Sign(anchor, tenantId);

            // Re-create with signature (since it's a record/immutable)
            var signedAnchor = anchor with { Signature = signature };

            // 6. External Persistence
            await _anchorStore.AppendAsync(signedAnchor, ct);
        }
    }
}
