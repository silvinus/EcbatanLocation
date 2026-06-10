using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.DTOs;

namespace EcbatanLocation.Application.Queries.GetStudios;

public record GetStudiosQuery : IRequest<IReadOnlyList<StudioDto>>;
