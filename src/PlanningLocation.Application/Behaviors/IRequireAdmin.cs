namespace PlanningLocation.Application.Behaviors;

/// <summary>
/// Marks a request that can only be executed by a user in the "Admin" role.
/// Enforced by the authorization pipeline behavior in the Web layer.
/// </summary>
public interface IRequireAdmin;
