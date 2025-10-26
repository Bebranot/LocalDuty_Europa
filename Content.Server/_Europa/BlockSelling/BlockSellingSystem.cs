using Content.Server.Cargo.Components;
using Content.Server.GameTicking;
using Content.Server.Station.Events;
using Content.Shared.GameTicking;
using Robust.Shared.Containers;

namespace Content.Server._Europa.BlockSelling;

public sealed class StationDontSellingSystems : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    private EntityQuery<StaticPriceComponent> _priseQuery;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BlockSellingGridComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BlockSellingStationComponent, StationPostInitEvent>(OnPostInit);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawning);

        _priseQuery = GetEntityQuery<StaticPriceComponent>();
    }

    private void OnStartup(EntityUid uid, BlockSellingGridComponent component, ref ComponentStartup args)
    {
        var entities = new HashSet<Entity<StaticPriceComponent>>();
        _lookup.GetChildEntities(uid, entities);

        foreach (var entityUid in entities)
        {
            DepreciatePrice(entityUid);
        }
    }

    private void DepreciatePrice(EntityUid uid)
    {
        if (_priseQuery.TryGetComponent(uid, out var priceComponent))
            priceComponent.Price = 0;

        if (!TryComp<ContainerManagerComponent>(uid, out var containers))
            return;

        foreach (var container in containers.Containers.Values)
        {
            foreach (var ent in container.ContainedEntities)
            {
                DepreciatePrice(ent);
            }
        }
    }

    private void OnPlayerSpawning(PlayerSpawnCompleteEvent ev)
    {
        if (!HasComp<BlockSellingStationComponent>(ev.Station))
            return;

        var entities = new HashSet<Entity<StaticPriceComponent>>();
        _lookup.GetChildEntities(ev.Mob, entities);

        foreach (var entityUid in entities)
        {
            DepreciatePrice(entityUid);
        }
    }

    private void OnPostInit(EntityUid uid, BlockSellingStationComponent component, ref StationPostInitEvent args)
    {
        foreach (var gridUid in args.Station.Comp.Grids)
        {
            AddComp<BlockSellingGridComponent>(gridUid);
        }
    }
}
