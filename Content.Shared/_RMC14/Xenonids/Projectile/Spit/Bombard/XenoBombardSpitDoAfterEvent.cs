using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Projectile.Spit.Bombard;

[Serializable, NetSerializable]
public sealed partial class XenoBombardSpitDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public NetCoordinates Coordinates;

    public XenoBombardSpitDoAfterEvent(NetCoordinates coordinates)
    {
        Coordinates = coordinates;
    }
}
