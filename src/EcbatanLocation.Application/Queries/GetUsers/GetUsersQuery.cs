using EcbatanLocation.Application.Behaviors;
using EcbatanLocation.Application.DTOs;
using EcbatanLocation.Application.Messaging;

namespace EcbatanLocation.Application.Queries.GetUsers;

public record GetUsersQuery : IRequest<IReadOnlyList<UserDto>>, IRequireAdmin;
