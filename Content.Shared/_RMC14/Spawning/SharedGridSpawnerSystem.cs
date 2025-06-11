using System.Numerics;
using Content.Shared._RMC14.Dropship;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.EntitySerialization.Systems;

namespace Content.Shared._RMC14.Spawning;

public abstract class SharedGridSpawnerSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private MapId? _map;
    private int _index;

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<GridSpawnerComponent, MapInitEvent>(OnGridSpawnerMapInit);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _map = null;
        _index = 0;
    }

    private void OnGridSpawnerMapInit(Entity<GridSpawnerComponent> ent, ref MapInitEvent args)
    {
        // 1. Проверяем, вызывается ли вообще этот метод для нашей сущности
        Log.Info($"GridSpawner MapInit triggered for entity: {ToPrettyString(ent)}");

        if (ent.Comp.Spawn is not { } spawn)
        {
            Log.Error($"GridSpawner on {ToPrettyString(ent)} has no 'Spawn' path defined!");
            return;
        }

        if (_net.IsClient)
        {
            // Эта проверка важна, спавн должен происходить только на сервере.
            // Мы не ожидаем увидеть это сообщение в логах сервера.
            Log.Info($"GridSpawner on {ToPrettyString(ent)} is on client, skipping.");
            return;
        }

        if (!_config.GetCVar(CCVars.GridFill))
        {
            // 2. Проверяем, действительно ли CVar является проблемой
            Log.Error($"GridSpawner on {ToPrettyString(ent)} is stopping because CCVars.GridFill is FALSE. Set 'gridfill true' in the server console and restart the round.");
            return;
        }

        if (_map == null)
        {
            _mapSystem.CreateMap(out var mapId);
            _map = mapId;
            Log.Info($"Created a new hidden map for GridSpawner, ID: {_map.Value}");
        }
        
        var offset = new Vector2(_index * 50, _index * 50);
        _index++;

        // 3. Самая важная проверка: загружается ли карта?
        if (!_mapSystem.MapExists(_map) ||
            !_mapLoader.TryLoadGrid(_map.Value, spawn, out var result, offset: offset))
        {
            Log.Error($"FAILED to load grid from path: '{spawn}' for entity {ToPrettyString(ent)}. Check that the path is correct and the YML file has no errors.");
            return;
        }

        var grid = result.Value;
        Log.Info($"SUCCESSFULLY loaded grid {ToPrettyString(grid)} from path '{spawn}'.");

        var xform = Transform(ent);
        var coordinates = _transform.GetMapCoordinates(ent, xform);
        coordinates = coordinates.Offset(ent.Comp.Offset);
        _transform.SetMapCoordinates(grid, coordinates);
        Log.Info($"Placed grid {ToPrettyString(grid)} at coordinates {coordinates}.");

        if (TryComp(grid, out PhysicsComponent? physics) &&
            TryComp(grid, out FixturesComponent? fixtures))
        {
            _physics.SetBodyType(grid, BodyType.Static, manager: fixtures, body: physics);
            _physics.SetBodyStatus(grid, physics, BodyStatus.OnGround);
            _physics.SetFixedRotation(grid, true, manager: fixtures, body: physics);
        }

        // 4. Проверяем, находится ли DropshipDestinationComponent и привязывается ли корабль
        if (TryComp(ent, out DropshipDestinationComponent? destination))
        {
            destination.Ship = grid;
            Dirty(ent, destination);
            Log.Info($"Found DropshipDestinationComponent on {ToPrettyString(ent)} and assigned Ship = {ToPrettyString(grid)}.");
        }
        else
        {
            Log.Warning($"GridSpawner {ToPrettyString(ent)} does not have a DropshipDestinationComponent to assign the ship to.");
        }
    }
}
