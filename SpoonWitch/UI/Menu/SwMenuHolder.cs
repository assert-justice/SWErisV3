using Eris;
using Prion.Node;
using SpoonWitch.Command;
using SpoonWitch.UI.Node;

namespace SpoonWitch.UI.Menu;

public class SwMenuHolder: SwUiNode
{
    private readonly List<string> MenuStack = [];
    private readonly SwCommandHandler CommandHandler;
    private SwMenu? CurrentMenu;
    private string? QueuedMenuId;

    public SwMenuHolder(PriNode node) : base(node)
    {
        CommandHandler = new(SwApp.CommandStore);
        CommandHandler.AddHandler("menu_set", SetMenu);
        CommandHandler.AddHandler("menu_back", (_)=>PopMenu());
    }

    // public SwMenuHolder()
    // {
    //     TryAddMenu(new SwMainMenu());
    // }
    public bool TryAddMenu(SwMenu menu)
    {
        AddChild(menu);
        if(MenuStack.Count == 0) PushMenu(menu.Id);
        return true;
    }
    public override void Update()
    {
        CommandHandler.Dispatch();
        HandleQueued();
        base.Update();
    }
    private void HandleQueued()
    {
        if(QueuedMenuId is null) return;
        if(CurrentMenu is null || QueuedMenuId != CurrentMenu.Id)
        {
            SwMenu? nextMenu = null;
            foreach (var item in GetChildren<SwMenu>())
            {
                item.Visible = false;
                if(item.Id == QueuedMenuId) nextMenu = item;
            }
            if(nextMenu is null) ErEngine.LogWarning("no menu with id '", QueuedMenuId, "' exists");
            else CurrentMenu = nextMenu;
        }
        QueuedMenuId = null;
    }
    private void PopMenu()
    {
        if(MenuStack.Count <= 1) return;
        MenuStack.RemoveAt(MenuStack.Count -1);
        QueuedMenuId = PeekMenu();
    }
    private void PushMenu(string menuName)
    {
        int idx = MenuStack.IndexOf(menuName);
        if(idx == -1) MenuStack.Add(menuName);
        else
        {
            while(idx < MenuStack.Count - 1) PopMenu();
        }
        QueuedMenuId = menuName;
    }
    private string PeekMenu()
    {
        return MenuStack[^1];
    }
    private void SetMenu(PriNode command)
    {
        if(!command.TryAs(out string menuName))
        {
            ErEngine.LogWarning("bad set menu command");
            return;
        }
        PushMenu(menuName);
    }
}