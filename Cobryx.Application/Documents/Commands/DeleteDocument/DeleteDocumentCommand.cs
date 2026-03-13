using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Documents.Commands.DeleteDocument;

public record DeleteDocumentCommand(Guid DocumentId) : IRequest<Result>;
