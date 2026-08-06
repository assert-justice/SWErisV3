using Eris;
using SpoonWitch.Command;
using SpoonWitch.UI.Node;

namespace SpoonWitch.UI.Menu;

public class SwMenuHolder: SwUiNode
{
    private readonly List<string> MenuStack = [];
    private readonly SwCommandHandler CommandHandler;
    private SwMenu? CurrentMenu;
    private string? QueuedMenuName;
    public SwMenuHolder()
    {
        CommandHandler = new(SwApp.CommandStore);
        CommandHandler.AddHandler("set_menu", SetMenu);
        TryAddMenu(new SwMainMenu());
    }
    public bool TryAddMenu(SwMenu menu)
    {
        AddChild(menu);
        if(MenuStack.Count == 0) PushMenu(menu.Name);
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
        if(QueuedMenuName is null) return;
        if(CurrentMenu is null || QueuedMenuName != CurrentMenu.Name)
        {
            SwMenu? nextMenu = null;
            foreach (var item in GetChildren<SwMenu>())
            {
                item.Visible = false;
                if(item.Name == QueuedMenuName) nextMenu = item;
            }
            if(nextMenu is null) ErEngine.LogWarning("no menu named '", QueuedMenuName, "' exists");
            else CurrentMenu = nextMenu;
        }
        QueuedMenuName = null;
    }
    private void PopMenu()
    {
        if(MenuStack.Count <= 1) return;
        MenuStack.RemoveAt(MenuStack.Count -1);
        QueuedMenuName = PeekMenu();
    }
    private void PushMenu(string menuName)
    {
        int idx = MenuStack.IndexOf(menuName);
        if(idx == -1) MenuStack.Add(menuName);
        else
        {
            while(idx < MenuStack.Count - 1) PopMenu();
        }
        QueuedMenuName = menuName;
    }
    private string PeekMenu()
    {
        return MenuStack[^1];
    }
    private void SetMenu(SwCommand command)
    {
        if(!command.Payload.TryAs(out string menuName))
        {
            ErEngine.LogWarning("bad set menu command");
            return;
        }
        PushMenu(menuName);
    }
}