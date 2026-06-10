using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.DTOs;

namespace EcbatanLocation.Application.Queries.GetOwnerByUserId;

public record GetOwnerByUserIdQuery(string UserId) : IRequest<OwnerDto?>;
