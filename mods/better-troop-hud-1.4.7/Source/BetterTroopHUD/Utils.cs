using TaleWorlds.Library;

namespace BetterTroopHUD;

public static class Utils
{
    public static void DisplayDebugMessage(string message)
    {
#if DEBUG
        InformationManager.DisplayMessage(new InformationMessage(message));
#endif
    }

    public static void DisplayMessage(string message)
    {
        InformationManager.DisplayMessage(new InformationMessage(message));
    }
}