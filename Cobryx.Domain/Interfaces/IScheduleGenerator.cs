using Cobryx.Domain.Entities;

namespace Cobryx.Domain.Interfaces;

public interface IScheduleGenerator
{
    IEnumerable<Installment> GenerateSchedule(Credit credit);
}
