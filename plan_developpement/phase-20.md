# Phase 20 : Unit of Work transactionnel & dispatch critique des domain events

**Objectif** : Passer d'un dispatch des domain events 100% post-commit (best-effort) à un flux transactionnel en deux temps : les handlers **critiques** (propagation de statut, cascade) tournent dans la même transaction que le handler de commande ; les handlers **best-effort** (notifications email, audit) tournent après le commit avec try/catch.

## Architecture

**Flux actuel :**

Handler → repo.SaveChanges() (self-managed transaction) → dispatch events (post-commit, best-effort)

**Flux cible :**

Mediator ouvre transaction via UoW → Handler tourne (repo fait SaveChanges sans gérer de transaction) → Critical event handlers tournent (même transaction) → UoW.CommitAsync() → Best-effort event handlers (post-commit, try/catch)

Si erreur avant commit → UoW.RollbackAsync()

## Analyse d'impact

| Couche | Fichiers impactés | Nature de l'impact |
|--------|-------------------|--------------------|
| **Application** | `ICriticalNotificationHandler.cs` (nouveau) | Nouvelle interface pour handlers transactionnels |
| **Application** | `IUnitOfWork.cs` (nouveau) | Abstraction Begin/Commit/Rollback |
| **Application** | `IDomainEventAccumulator.cs` | Ajouter `StoreForBestEffort()` / `CollectBestEffort()` |
| **Application** | `Mediator.cs` | Refactorer `ExecuteHandler` : transaction UoW + dispatch en deux phases |
| **Application** | `AcceptReservationCommandHandler.cs` | Simplifier : retirer la propagation manuelle (déléguée au critical handler) |
| **Application** | `ConfirmReservationCommandHandler.cs` | Idem |
| **Application** | `StatusPropagationHandler.cs` (nouveau) | `ICriticalNotificationHandler` pour `ReservationAccepted` / `ReservationConfirmed` |
| **Application** | `MessagingServiceCollectionExtensions.cs` | Scanner et enregistrer `ICriticalNotificationHandler<>` |
| **Infrastructure** | `UnitOfWork.cs` (nouveau) | Implémentation EF Core de `IUnitOfWork` |
| **Infrastructure** | `ReservationRepository.cs` | Retirer les transactions self-managed |
| **Infrastructure** | `DependencyInjection.cs` | Enregistrer `IUnitOfWork → UnitOfWork` (Scoped) |

---

### Commit 1/4 — Application : nouvelles interfaces

> `feat(app): add ICriticalNotificationHandler, IUnitOfWork and extend IDomainEventAccumulator`

#### 1.1 `ICriticalNotificationHandler.cs`

Fichier : `src/EcbatanLocation.Application/Messaging/ICriticalNotificationHandler.cs`

```csharp
namespace EcbatanLocation.Application.Messaging;

public interface ICriticalNotificationHandler<in TNotification>
    where TNotification : INotification
{
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}
```

#### 1.2 `IUnitOfWork.cs`

Fichier : `src/EcbatanLocation.Application/Messaging/IUnitOfWork.cs`

```csharp
namespace EcbatanLocation.Application.Messaging;

public interface IUnitOfWork
{
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
```

#### 1.3 `IDomainEventAccumulator.cs` modifié

Fichier : `src/EcbatanLocation.Application/Messaging/IDomainEventAccumulator.cs`

Ajouter deux méthodes :

- `void StoreForBestEffort(IReadOnlyList<IDomainEvent> events)` — sauvegarde les events collectés pour le dispatch post-commit
- `IReadOnlyList<IDomainEvent> CollectBestEffort()` — récupère les events stockés pour le dispatch best-effort

#### 1.4 `MessagingServiceCollectionExtensions.cs` modifié

Fichier : `src/EcbatanLocation.Application/Messaging/MessagingServiceCollectionExtensions.cs`

Étendre le scan d'assembly pour enregistrer aussi les `ICriticalNotificationHandler<>` en plus des `INotificationHandler<>`.

---

### Commit 2/4 — Infrastructure : UnitOfWork + simplification repository

> `feat(infra): add UnitOfWork, remove self-managed transactions from repository`

#### 2.1 `UnitOfWork.cs`

Fichier : `src/EcbatanLocation.Infrastructure/Persistence/UnitOfWork.cs`

```csharp
using Microsoft.EntityFrameworkCore.Storage;
using EcbatanLocation.Application.Messaging;

namespace EcbatanLocation.Infrastructure.Persistence;

public class UnitOfWork(EcbatanLocationDbContext context) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    public async Task BeginTransactionAsync(CancellationToken ct = default)
        => _transaction = await context.Database.BeginTransactionAsync(ct);

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(ct);
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(ct);
            _transaction = null;
        }
    }
}
```

#### 2.2 `ReservationRepository.cs` simplifié

Fichier : `src/EcbatanLocation.Infrastructure/Repositories/ReservationRepository.cs`

Retirer les `BeginTransactionAsync` / `CommitAsync` self-managed dans `AddAsync` et `UpdateAsync`. Le guard overlap + `SaveChangesAsync` suffisent — la transaction ambiante du UoW protège l'atomicité.

```csharp
public async Task AddAsync(Reservation reservation, CancellationToken ct = default)
{
    await GuardNoOverlapAsync(reservation, ct);
    await context.Reservations.AddAsync(reservation, ct);
    await SaveTranslatingOverlapAsync(ct);
}
```

#### 2.3 `DependencyInjection.cs` modifié

Fichier : `src/EcbatanLocation.Infrastructure/DependencyInjection.cs`

Ajouter : `services.AddScoped<IUnitOfWork, UnitOfWork>();`

---

### Commit 3/4 — Application : Mediator transactionnel + StatusPropagationHandler

> `feat(app): transactional mediator dispatch and StatusPropagationHandler`

#### 3.1 `Mediator.cs` refactoré

Fichier : `src/EcbatanLocation.Application/Messaging/Mediator.cs`

`ExecuteHandler` devient :

```csharp
private async Task<TResponse> ExecuteHandler<TResponse>(
    IRequest<TResponse> request, CancellationToken cancellationToken)
{
    using var scope = scopeFactory.CreateScope();
    var scopedProvider = scope.ServiceProvider;
    var unitOfWork = scopedProvider.GetRequiredService<IUnitOfWork>();

    await unitOfWork.BeginTransactionAsync(cancellationToken);
    try
    {
        // 1. Run handler
        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        var handler = scopedProvider.GetRequiredService(handlerType);
        var handleMethod = handlerType.GetMethod("Handle")!;
        var response = await (Task<TResponse>)handleMethod.Invoke(
            handler, [request, cancellationToken])!;

        // 2. Critical event handlers (same transaction — failure = rollback)
        await DispatchCriticalEventsAsync(scopedProvider, cancellationToken);

        // 3. Commit
        await unitOfWork.CommitAsync(cancellationToken);

        // 4. Best-effort event handlers (post-commit, swallowed errors)
        await DispatchBestEffortEventsAsync(scopedProvider, cancellationToken);

        return response;
    }
    catch
    {
        await unitOfWork.RollbackAsync(cancellationToken);
        throw;
    }
}
```

Dispatch split en deux méthodes privées :

```csharp
private static async Task DispatchCriticalEventsAsync(
    IServiceProvider sp, CancellationToken ct)
{
    var accumulator = sp.GetRequiredService<IDomainEventAccumulator>();
    var events = accumulator.Collect();
    if (events.Count == 0) return;

    accumulator.StoreForBestEffort(events);

    foreach (var domainEvent in events)
    {
        var notificationType = typeof(DomainEventNotification<>)
            .MakeGenericType(domainEvent.GetType());
        var notification = (INotification)Activator
            .CreateInstance(notificationType, domainEvent)!;

        var handlerType = typeof(ICriticalNotificationHandler<>)
            .MakeGenericType(notificationType);
        var handleMethod = handlerType.GetMethod("Handle")!;

        foreach (var handler in sp.GetServices(handlerType))
            await (Task)handleMethod.Invoke(handler!, [notification, ct])!;
    }
}

private static async Task DispatchBestEffortEventsAsync(
    IServiceProvider sp, CancellationToken ct)
{
    var accumulator = sp.GetRequiredService<IDomainEventAccumulator>();
    var events = accumulator.CollectBestEffort();

    // Même logique que le dispatch actuel mais avec try/catch par handler
    foreach (var domainEvent in events)
    {
        var notificationType = typeof(DomainEventNotification<>)
            .MakeGenericType(domainEvent.GetType());
        var notification = (INotification)Activator
            .CreateInstance(notificationType, domainEvent)!;

        var handlerType = typeof(INotificationHandler<>)
            .MakeGenericType(notificationType);
        var handleMethod = handlerType.GetMethod("Handle")!;

        foreach (var handler in sp.GetServices(handlerType))
        {
            try
            {
                await (Task)handleMethod.Invoke(handler!, [notification, ct])!;
            }
            catch
            {
                // Best-effort : log and swallow
            }
        }
    }
}
```

#### 3.2 `StatusPropagationHandler.cs`

Fichier : `src/EcbatanLocation.Application/EventHandlers/StatusPropagationHandler.cs`

```csharp
public sealed class StatusPropagationHandler(
    IReservationRepository reservationRepository,
    ReservationDomainService domainService)
    : ICriticalNotificationHandler<DomainEventNotification<ReservationAccepted>>,
      ICriticalNotificationHandler<DomainEventNotification<ReservationConfirmed>>
{
    public async Task Handle(
        DomainEventNotification<ReservationAccepted> notification,
        CancellationToken ct)
        => await PropagateAsync(notification.DomainEvent.ReservationId, ct);

    public async Task Handle(
        DomainEventNotification<ReservationConfirmed> notification,
        CancellationToken ct)
        => await PropagateAsync(notification.DomainEvent.ReservationId, ct);

    private async Task PropagateAsync(Guid parentId, CancellationToken ct)
    {
        var dependents = await reservationRepository
            .GetDependentsByParentIdAsync(parentId, ct);
        if (dependents.Count == 0) return;

        var parent = await reservationRepository.GetByIdAsync(parentId, ct)
            ?? throw new InvalidOperationException(
                $"Reservation '{parentId}' not found.");

        domainService.PropagateStatusToDependents(parent, dependents);
        await reservationRepository.UpdateRangeAsync(dependents, ct);
    }
}
```

#### 3.3 `AcceptReservationCommandHandler.cs` simplifié

Fichier : `src/EcbatanLocation.Application/Commands/AcceptReservation/AcceptReservationCommandHandler.cs`

Retirer la logique de propagation (chargement dépendantes + `PropagateStatusToDependents` + `UpdateRangeAsync`). Le handler fait uniquement :

1. Guard si dépendante
2. `reservation.Accept(request.AcceptedBy)`
3. `UpdateAsync(reservation)`

La propagation est désormais gérée par `StatusPropagationHandler` via le domain event `ReservationAccepted`, dans la même transaction.

#### 3.4 `ConfirmReservationCommandHandler.cs` simplifié

Même simplification que Accept : retirer la propagation manuelle. Le `StatusPropagationHandler` gère via `ReservationConfirmed`.

---

### Commit 4/4 — Tests

> `test: add tests for transactional UoW and critical event dispatch`

#### 4.1 Tests Mediator

Fichier : `tests/EcbatanLocation.Application.Tests/Messaging/`

- Transaction rollback : un critical handler qui échoue annule le `SaveChanges` du handler
- Transaction commit : handler + critical handler OK → données persistées
- Best-effort post-commit : un `INotificationHandler` qui échoue ne rollback pas

#### 4.2 Tests StatusPropagationHandler

Fichier : `tests/EcbatanLocation.Application.Tests/EventHandlers/`

- Accept parent → dépendantes passent en Accepted (via event, pas via handler direct)
- Confirm parent → dépendantes passent en Confirmed
- Parent sans dépendantes → no-op

#### 4.3 Tests handlers simplifiés

- `AcceptReservationCommandHandler` : vérifier que la propagation se fait via event (le handler ne charge plus les dépendantes)
- `ConfirmReservationCommandHandler` : idem

#### 4.4 Tests ReservationRepository

- `AddAsync` fonctionne sans transaction self-managed (transaction ambiante UoW)
- `UpdateAsync` idem

---

## Points d'attention

- **Backward compatible** : les `INotificationHandler<>` existants (email, audit) continuent de fonctionner en best-effort post-commit. Aucune modification nécessaire.
- **Pas de double dispatch** : les events sont collectés une seule fois après le handler. Les critical handlers ne doivent pas émettre de nouveaux domain events (sinon boucle).
- **Idempotence** : si un critical handler échoue, toute la transaction est rollback. L'utilisateur voit une erreur et peut réessayer.
- **Tests** : le `IntegrationTestFixture` utilise SQLite in-memory, les transactions y fonctionnent normalement.

**Livrable** : Domain events critiques exécutés dans la même transaction que le handler, best-effort post-commit, handlers de commande simplifiés.
