using Concordia;
using Cobryx.Domain.Common;

namespace Cobryx.Application.Documents.Commands.DeleteDocument;

public record DeleteDocumentCommand(Guid DocumentId) : IRequest<Result>;
