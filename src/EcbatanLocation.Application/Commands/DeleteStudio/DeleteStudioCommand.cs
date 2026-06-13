using EcbatanLocation.Application.Behaviors;
using EcbatanLocation.Application.Messaging;

namespace EcbatanLocation.Application.Commands.DeleteStudio;

public record DeleteStudioCommand(Guid StudioId) : IRequest, IRequireAdmin;
