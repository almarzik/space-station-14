// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
// Ported from CrystallEdge, TheShuEd and contributors, revision 728cd277da. Original authorship retained; SS220 notice covers adaptations.

using Content.Client.CharacterInfo;
using Content.Shared.SS220.Ambitions;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.Ambitions;

[UsedImplicitly]
public sealed class SS220AmbitionsBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private SS220AmbitionsMenu? _menu;

    public SS220AmbitionsBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SS220AmbitionsMenu>();

        _menu.OnNewAmbitionRequest += revision => SendMessage(new SS220AmbitionCreateMessage(revision));
        _menu.OnLockAmbitionRequest += revision => SendMessage(new SS220AmbitionLockMessage(revision));
        _menu.OnDeleteAmbitionRequest += (prototype, revision) => SendMessage(new SS220AmbitionDeleteMessage(prototype, revision));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_menu == null)
            return;

        if (state is not SS220AmbitionsBuiState msg)
            return;

        _menu.Update(msg);
        EntMan.System<CharacterInfoSystem>().RequestCharacterInfo();
    }
}
