using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Projectile.Spit.Bombard;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoSpitSystem))]
public sealed partial class XenoBombardSpitComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaCost = 200;

    [DataField, AutoNetworkedField]
    public float Speed = 30;

    [DataField, AutoNetworkedField]
    public TimeSpan BombardDelay = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public EntProtoId ProjectileId = "XenoBombardSpitProjectile";

    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("XenoSpitAcid", AudioParams.Default.WithVolume(-10f));
}
