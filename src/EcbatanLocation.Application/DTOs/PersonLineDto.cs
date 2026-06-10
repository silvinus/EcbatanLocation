using EcbatanLocation.Domain.Enums;

namespace EcbatanLocation.Application.DTOs;

public record PersonLineDto(ClientType ClientType, int AdultCount, int ChildrenUnder3Count);
