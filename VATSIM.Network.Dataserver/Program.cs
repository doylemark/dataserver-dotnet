using System;
using System.Threading;
using System.Threading.Tasks;

namespace VATSIM.Network.Dataserver
{
    public static class Program
    {
        private static readonly FeedVersion3 FeedVersion3 = new FeedVersion3();

        private static async Task Main()
        {
            FeedVersion3.StartFeedVersion3();

            Console.WriteLine("Feeds Started..");

            await Task.Run(() => Thread.Sleep(Timeout.Infinite));
        }
    }
}