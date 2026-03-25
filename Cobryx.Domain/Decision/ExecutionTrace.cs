using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Cobryx.Domain.Decision;

public class ExecutionStep
{
    public string StepName { get; set; } = string.Empty;
    public decimal InputValue { get; set; }
    public decimal OutputValue { get; set; }
    public string Description { get; set; } = string.Empty;

    public ExecutionStep() { }

    public ExecutionStep(string name, decimal input, decimal output, string description = "")
    {
        StepName = name;
        InputValue = input;
        OutputValue = output;
        Description = description;
    }
}

public class ExecutionTrace
{
    public List<ExecutionStep> Steps { get; set; } = new();

    public void AddStep(string name, decimal input, decimal output, string description = "")
    {
        Steps.Add(new ExecutionStep(name, input, output, description));
    }

    public string GetTraceHash(string engineVersion = "")
    {
        var payload = new
        {
            Version = engineVersion,
            Steps = Steps
        };
        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
