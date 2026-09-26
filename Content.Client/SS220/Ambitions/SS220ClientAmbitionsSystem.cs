// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
// Adapted from CrystallEdge ambitions by TheShuEd and contributors (728cd277da).

using Content.Client.CharacterInfo;
using Content.Shared.SS220.Ambitions;
using Content.Shared.SS220.Ambitions.Components;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.SS220.Ambitions;

public sealed class SS220ClientAmbitionsSystem : SS220SharedAmbitionsSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SS220AmbitionsSetupComponent, CharacterInfoSystem.GetCharacterInfoControlsEvent>(OnControls);
    }

    private void OnControls(Entity<SS220AmbitionsSetupComponent> ent, ref CharacterInfoSystem.GetCharacterInfoControlsEvent args)
    {
        if (ent.Comp.Locked)
            return;

        var button = new Button
        {
            Text = Loc.GetString("ss220-ambitions-ui-button-open"),
            Margin = new Thickness(5),
        };
        button.OnPressed += _ => RaiseNetworkEvent(new SS220ToggleAmbitionsScreenEvent());
        args.Controls.Add(button);
    }
}
