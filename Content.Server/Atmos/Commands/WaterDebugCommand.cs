// Content.Server/Atmos/Commands/WaterDebugCommand.cs
using Robust.Shared.Console;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;  // Добавлено для Gas.Water и Atmospherics

namespace Content.Server.Atmos.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class WaterDebugCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public string Command => "showwater";
    public string Description => "Показывает количество воды на тайлах";
    public string Help => "showwater";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var atmosSystem = _entManager.System<AtmosphereSystem>();
        var player = shell.Player;

        if (player?.AttachedEntity is not { Valid: true } playerEnt)
        {
            shell.WriteLine("Вы не привязаны к сущности");
            return;
        }

        var xform = _entManager.GetComponent<TransformComponent>(playerEnt);
        if (xform.GridUid == null)
        {
            shell.WriteLine("Вы не на сетке");
            return;
        }

        var tile = atmosSystem.GetTileMixture(playerEnt);
        var waterAmount = tile?.GetMoles(Content.Shared.Atmos.Gas.Water) ?? 0;  // Полный путь

        shell.WriteLine($"Воды в текущем тайле: {waterAmount:F2} молей");

        if (waterAmount > 0)
        {
            shell.WriteLine($"Давление воды: {CalculateWaterPressure(waterAmount, tile?.Temperature ?? 0):F2} кПа");
        }
    }

    private float CalculateWaterPressure(float moles, float temperature)
    {
        return moles * Atmospherics.R * temperature / Atmospherics.CellVolume;
    }
}
