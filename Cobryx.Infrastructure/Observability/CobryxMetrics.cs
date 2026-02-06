using System.Diagnostics.Metrics;

namespace Cobryx.Infrastructure.Observability;

public class CobryxMetrics
{
    public const string MeterName = "Cobryx.Core";
    private readonly Meter _meter;
    private readonly Counter<long> _businessOutcomesCounter;
    private readonly Counter<long> _domainErrorsCounter;

    public CobryxMetrics()
    {
        _meter = new Meter(MeterName, "1.0.0");
        _businessOutcomesCounter = _meter.CreateCounter<long>(
            "cobryx_business_outcomes_total",
            description: "Total number of successful business outcomes");

        _domainErrorsCounter = _meter.CreateCounter<long>(
            "cobryx_domain_errors_total",
            description: "Total number of domain errors");
    }

    public void RecordOutcome(string outcomeCode)
    {
        var module = GetModule(outcomeCode);
        _businessOutcomesCounter.Add(1,
            new KeyValuePair<string, object?>("code", outcomeCode),
            new KeyValuePair<string, object?>("module", module.ToString()));
    }

    public void RecordError(string errorCode, int? numericCode = null)
    {
        var module = GetModule(errorCode);
        _domainErrorsCounter.Add(1,
            new KeyValuePair<string, object?>("code", errorCode),
            new KeyValuePair<string, object?>("module", module.ToString()),
            new KeyValuePair<string, object?>("numeric_code", numericCode));
    }

    private static Cobryx.Domain.Enums.CobryxModule GetModule(string code)
    {
        if (string.IsNullOrEmpty(code)) return Cobryx.Domain.Enums.CobryxModule.System;

        var prefix = code.Split('.')[0].ToUpperInvariant();

        if (Enum.TryParse<Cobryx.Domain.Enums.CobryxModule>(prefix, true, out var module))
        {
            return module;
        }

        return Cobryx.Domain.Enums.CobryxModule.Other;
    }
}
