using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.DTOs;

namespace EcbatanLocation.Application.Queries.GetOwners;

public record GetOwnersQuery : IRequest<IReadOnlyList<OwnerDto>>;
