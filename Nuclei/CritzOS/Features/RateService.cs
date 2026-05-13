using NuclearOption.Networking;

namespace Nuclei.CritzOS.Features;

/// <summary>
/// Used for managing end-of-mission feedback
/// </summary>
public class RateService
{
    private static bool _isLocked;

    static RateService()
    {
        _isLocked = true;
    }
    
    public static void Unlock()
    {
        _isLocked = false;
    }
    
    public static void Lock()
    {
        _isLocked = true;
    }

    /*
    public static void ReportFeedback(Player player, int rate)
    {
        ReportCommandService.SendFeedback(player.PlayerName, rate);
    }
    */
    
}