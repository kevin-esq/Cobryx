namespace Cobryx.Application.Collections.Assignment;

public interface IAssignmentEngine
{
    public System.Threading.Tasks.Task AssignCasesAsync(System.Guid tenantId);
}
