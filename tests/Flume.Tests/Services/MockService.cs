using System.Threading.Tasks;

namespace Flume.Tests.Services;

internal interface IMockService
{
    void SyncTask();

    Task AsyncTask();
}


internal sealed class MockService : IMockService
{
    public void SyncTask()
    {
        // Simulate some synchronous work
        System.Threading.Thread.Sleep(500);
    }
    public async Task AsyncTask()
    {
        // Simulate some asynchronous work
        await Task.Delay(500).ConfigureAwait(false);
    }
}