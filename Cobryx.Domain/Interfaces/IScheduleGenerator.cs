using Cobryx.Domain.Entities.Invoicing;

namespace Cobryx.Domain.Interfaces;

public interface IScheduleGenerator
{
    IEnumerable<Installment> GenerateSchedule(Entities.Credit credit);
}
