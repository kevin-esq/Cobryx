namespace Cobryx.Application.Common.Models;

public record PaginatedList<T>(IEnumerable<T> Items, int TotalCount, int Page, int TotalPages);
