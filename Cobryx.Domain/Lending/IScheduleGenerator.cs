namespace Cobryx.Domain.Lending;

public interface IScheduleGenerator
{
    public IEnumerable<Installment> GenerateSchedule(Credit credit);
}
