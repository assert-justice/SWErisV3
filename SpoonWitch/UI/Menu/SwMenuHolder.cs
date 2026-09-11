using Eris;
using Prion.Node;
using SpoonWitch.Command;
using SpoonWitch.UI.Node;

namespace SpoonWitch.UI.Menu;

public class SwMenuHolder: SwUiNode
{
    private readonly Stack<string> MenuStack = [];
    private readonly SwCommandHandler CommandHandler;
    private readonly Dictionary<string,SwMenu> MenuLookup = [];
    private SwMenu? CurrentMenu;
    public SwMenuHolder(PriNode node) : base(node)
    {
        CommandHandler = new(SwApp.CommandStore);
        CommandHandler.AddHandler("menu_set", SetMenu);
        CommandHandler.AddHandler("menu_back", (_)=>PopMenu());
        foreach (var menu in GetChildren<SwMenu>())
        {
            if(MenuStack.Count == 0) MenuStack.Push(menu.Id);
            if(!MenuLookup.TryAdd(menu.Id, menu)) ErEngine.LogWarning("duplicate menu id ", menu.Id);
            menu.Visible = false;
        }
    }
    public override void Update()
    {
        CommandHandler.Dispatch();
        HandleQueued();
        base.Update();
    }
    private void HandleQueued()
    {
        if(!TryPeek(out string menuId)) return;
        if(CurrentMenu is null || CurrentMenu.Id != menuId)
        {
            if(!MenuLookup.TryGetValue(menuId, out var nextMenu))
            {
                ErEngine.LogWarning("invalid menu name ", menuId);
                return;
            }
            CurrentMenu?.Visible = false;
            CurrentMenu = nextMenu;
            nextMenu.Visible = true;
        }
    }
    private void PopMenu()
    {
        if(MenuStack.Count <= 1) return;
        MenuStack.Pop();
    }
    private bool TryPeek(out string menuId)
    {
        menuId = string.Empty;
        if(!MenuStack.TryPeek(out var id)) return false;
        menuId = id;
        return true;
    }
    private void SetMenu(PriNode command)
    {
        if(!command.TryGet("menu_id", out string menuId))
        {
            ErEngine.LogWarning("bad set menu command");
            return;
        }
        if (!MenuLookup.ContainsKey(menuId))
        {
            ErEngine.LogWarning("invalid menu name ", menuId);
            return;
        }
        if(CurrentMenu is not null && CurrentMenu.Id == menuId) return;
        if (MenuStack.Contains(menuId))
        {
            while(MenuStack.TryPop(out var id) && id != menuId){}
            MenuStack.Push(menuId);
        }
    }
}