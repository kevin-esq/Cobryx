using Cobryx.Domain.Shared;
using Cobryx.Application.Common.Interfaces;

using Concordia;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Documents.Commands.DeleteDocument;

[TenantScoped]
public record DeleteDocumentCommand(Guid DocumentId) : IRequest<Result>, IRequiresTenant;
