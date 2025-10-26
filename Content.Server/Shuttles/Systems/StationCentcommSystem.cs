using Content.Server._Europa.BlockSelling;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Server.Shuttles.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Shuttles.Systems;

public sealed partial class StationCentCommSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("station.centcomm");
        SubscribeLocalEvent<StationCentcommComponent, ComponentShutdown>(OnCentcommShutdown);
        SubscribeLocalEvent<StationCentcommComponent, ComponentInit>(OnCentcommInit);
    }

    private void OnCentcommShutdown(EntityUid uid, StationCentcommComponent component, ComponentShutdown args)
    {
        QueueDel(component.Entity);
        component.Entity = EntityUid.Invalid;

        if (_map.MapExists(component.MapId))
            _map.DeleteMap(component.MapId);

        component.MapId = MapId.Nullspace;
    }

    private void OnCentcommInit(EntityUid uid, StationCentcommComponent component, ComponentInit args)
    {
        // Post mapinit? fancy
        if (TryComp<TransformComponent>(component.Entity, out var xform))
        {
            component.MapId = xform.MapID;
            return;
        }

        AddCentcomm(component);
    }

    private void AddCentcomm(StationCentcommComponent component)
    {
        var query = AllEntityQuery<StationCentcommComponent>();

        while (query.MoveNext(out var otherComp))
        {
            if (otherComp == component)
                continue;

            component.MapId = otherComp.MapId;
            return;
        }

        if (_prototypeManager.TryIndex<GameMapPrototype>(component.Station, out var gameMap))
        {
            _gameTicker.LoadGameMap(gameMap, out var mapId);

            if (_shuttle.TryAddFTLDestination(mapId, true, out var ftlDestination))
                ftlDestination.Whitelist = component.ShuttleWhitelist;

            foreach (var uid in _mapMan.GetAllGrids(mapId))
            {
                EnsureComp<BlockSellingStationComponent>(uid);
            }

            _map.InitializeMap(mapId);
        }
        else
        {
            _sawmill.Warning("No Centcomm map found, skipping setup.");
        }
    }
}
