using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._BaroStation.NuclearReactor;

[NetworkedComponent, RegisterComponent, AutoGenerateComponentState]
[Access(typeof(SharedNuclearReactorConsoleSystem))]
public sealed partial class NuclearReactorConsoleComponent : Component
{
    [DataField("linkedReactor"), AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? LinkedReactor;

    [DataField("lastReactorState"), AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public NuclearReactorUiState? LastReactorState;

    [DataField]
    public ProtoId<SinkPortPrototype> LinkPort = "NuclearReactorLink";
}

[Serializable, NetSerializable]
public enum NuclearReactorConsoleUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class NuclearReactorConsoleUiState : BoundUserInterfaceState
{
    public bool HasReactor;
    public bool ReactorEnabled;
    public float CurrentTemperature;
    public float TargetTemperature;
    public float PowerOutput;
    public float Integrity;
    public ContainerInfo[] RodSlots;
    public float OptimalTemperature;
    public float CriticalTemperature;
    public int CoolingLevel;
    public bool HasDepletedRod;
    public bool HasPower;

    public NuclearReactorConsoleUiState(
        bool hasReactor,
        bool reactorEnabled,
        float curTemp,
        float tarTemp,
        float power,
        float integrity,
        ContainerInfo[] slots,
        float optTemp,
        float critTemp,
        int coolingLevel,
        bool hasDepletedRod,
        bool hasPower)
    {
        HasReactor = hasReactor;
        ReactorEnabled = reactorEnabled;
        CurrentTemperature = curTemp;
        TargetTemperature = tarTemp;
        PowerOutput = power;
        Integrity = integrity;
        RodSlots = slots;
        OptimalTemperature = optTemp;
        CriticalTemperature = critTemp;
        CoolingLevel = coolingLevel;
        HasDepletedRod = hasDepletedRod;
        HasPower = hasPower;
    }
}

[Serializable, NetSerializable]
public sealed class NuclearReactorConsoleLinkMessage : BoundUserInterfaceMessage
{
    public NetEntity Target;

    public NuclearReactorConsoleLinkMessage(NetEntity target)
    {
        Target = target;
    }
}

[Serializable, NetSerializable]
public sealed class NuclearReactorConsoleClearLinkMessage : BoundUserInterfaceMessage { }
