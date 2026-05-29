using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Nuclei.Helpers;

namespace Nuclei.CritzOS;

public class RestartService
{
    private static CancellationTokenSource? _restartCts;
    
    /// <summary>
    /// Checks player count. If it's 0, start the restart timer.
    /// </summary>
    public static void CheckIfNoPlayers()
    {
        if (PlayerUtils.GetPlayerCount() == 0)
        {
            // Only start the timer if one isn't already running
            if (_restartCts == null)
            {
                _restartCts = new CancellationTokenSource();
                _ = ScheduleRestartAsync(_restartCts.Token);
            }
        }
    }

    public static void CancelRestart()
    {
        // Player joined — cancel any pending restart
        if (_restartCts != null)
        {
            Nuclei.Logger?.LogInfo($"A Player joined. Restart canceled");
            _restartCts?.Cancel();
            _restartCts = null;
        }
    }

    private static async Task ScheduleRestartAsync(CancellationToken ct)
    {
        try
        {
            Nuclei.Logger?.LogInfo($"No players. Waiting 60 seconds to restart...");
            await Task.Delay(60000, ct);

            // Re-check after delay
            if (PlayerUtils.GetPlayerCount() == 0)
            {
                Nuclei.Logger?.LogInfo("RESTARTING SERVER...");
                Process.Start("/usr/bin/bash",
                    $"-c \"sudo systemctl restart nuclear_option_{CritzOSGlobals.ServerName.ToUpper()}\"");
            }
        }
        catch (TaskCanceledException)
        {
            // Players rejoined before restart — do nothing
        }
        catch (Exception e)
        {
            Nuclei.Logger?.LogError(e);
        }
        finally
        {
            _restartCts = null;
        }
    }
}