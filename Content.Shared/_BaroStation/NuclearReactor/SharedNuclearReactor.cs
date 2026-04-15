using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Content.Shared.Containers.ItemSlots;

namespace Content.Shared._BaroStation.NuclearReactor;

[NetworkedComponent, RegisterComponent, AutoGenerateComponentState]
[Access(typeof(SharedNuclearReactorSystem))]
public sealed partial class NuclearReactorComponent : Component
{
    public const string RodSlot1Id = "rod_slot_1";
    public const string RodSlot2Id = "rod_slot_2";
    public const string RodSlot3Id = "rod_slot_3";
    public const string RodSlot4Id = "rod_slot_4";

    [DataField("rodSlot1"), AutoNetworkedField]
    public ItemSlot RodSlot1 = new();

    [DataField("rodSlot2"), AutoNetworkedField]
    public ItemSlot RodSlot2 = new();

    [DataField("rodSlot3"), AutoNetworkedField]
    public ItemSlot RodSlot3 = new();

    [DataField("rodSlot4"), AutoNetworkedField]
    public ItemSlot RodSlot4 = new();

    [DataField("currentTemperature"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float CurrentTemperature = 293.15f;

    [DataField("targetTemperature"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float TargetTemperature = 500f;

    [DataField("maxPowerOutput")]
    public float MaxPowerOutput = 250000f;

    [DataField("integrity"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float Integrity = 100f;

    [DataField("enabled"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Enabled = false;

    [DataField("thermalInertia")]
    public float ThermalInertia = 0.05f;

    [DataField("nextUpdate"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField("updateInterval")]
    public float UpdateInterval = 1.0f;

    [DataField("meltdownSound")]
    public SoundSpecifier? MeltdownSound = new SoundPathSpecifier("/Audio/Effects/meltdown.ogg");

    /// <summary>
    /// Уровень охлаждения (1-4). Зависит от количества стержней.
    /// </summary>
    [DataField("coolingLevel"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public int CoolingLevel = 1;

    /// <summary>
    /// Оптимальная температура (вычисляется динамически)
    /// </summary>
    [DataField("optimalTemperature"), ViewVariables, AutoNetworkedField]
    public float OptimalTemperature = 1000f;
}
[Serializable, NetSerializable]
public sealed class NuclearReactorSetCoolingMessage : BoundUserInterfaceMessage
{
    public int CoolingLevel;
    public NuclearReactorSetCoolingMessage(int level) => CoolingLevel = level;
}
[Serializable, NetSerializable]
public sealed class NuclearReactorUiState : BoundUserInterfaceState
{
    public bool Enabled;
    public float CurrentTemperature;
    public float TargetTemperature;
    public float PowerOutput;
    public float Integrity;
    public ContainerInfo[] RodSlots;
    public float OptimalTemperature;
    public float CriticalTemperature;
    public int CoolingLevel;

    public NuclearReactorUiState(bool enabled, float curTemp, float tarTemp, float power,
        float integrity, ContainerInfo[] slots, float optTemp, float critTemp, int coolingLevel)
    {
        Enabled = enabled;
        CurrentTemperature = curTemp;
        TargetTemperature = tarTemp;
        PowerOutput = power;
        Integrity = integrity;
        RodSlots = slots;
        OptimalTemperature = optTemp;
        CriticalTemperature = critTemp;
        CoolingLevel = coolingLevel;
    }
}

[Serializable, NetSerializable]
public sealed class ContainerInfo
{
    public bool HasItem;
    public string? ItemName;
    public float? FuelLeft;

    public ContainerInfo(bool hasItem, string? name, float? fuel)
    {
        HasItem = hasItem;
        ItemName = name;
        FuelLeft = fuel;
    }
}

[Serializable, NetSerializable]
public sealed class NuclearReactorSetTemperatureMessage : BoundUserInterfaceMessage
{
    public float Temperature;
    public NuclearReactorSetTemperatureMessage(float temp) => Temperature = temp;
}

[Serializable, NetSerializable]
public sealed class NuclearReactorToggleMessage : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public sealed class NuclearReactorEjectMessage : BoundUserInterfaceMessage
{
    public int Slot;
    public NuclearReactorEjectMessage(int slot) => Slot = slot;
}

[Serializable, NetSerializable]
public enum NuclearReactorUiKey : byte
{
    Key
}
