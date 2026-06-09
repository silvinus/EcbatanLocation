using PlanningLocation.Domain.Enums;

namespace PlanningLocation.Application.DTOs;

public record PersonLineDto(ClientType ClientType, int AdultCount, int ChildrenUnder3Count);
