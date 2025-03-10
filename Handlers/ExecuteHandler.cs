using ExileCore.Shared;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyLittleCrafter.Handlers;

public static class ExecuteHandler
{
    public static async SyncTask<bool> AsyncExecuteWithCancellationHandling(Func<bool> condition, int timeoutS, CancellationToken token)
    {
        using var ctsTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        ctsTimeout.CancelAfter(TimeSpan.FromSeconds(timeoutS));

        try
        {
            while (!ctsTimeout.Token.IsCancellationRequested)
            {
                if (condition())
                {
                    return true;
                }
                await Task.Delay(StateHandler.GetServerLatency(), ctsTimeout.Token);
            }

            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}