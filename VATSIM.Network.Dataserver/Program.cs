using System;
using System.Threading;
using System.Threading.Tasks;

namespace VATSIM.Network.Dataserver
{
    public class Program
    {
        private static FeedVersion1 feedVersion1 = new FeedVersion1();
        private static FeedVersion3 feedVersion3 = new FeedVersion3();

        private async static Task Main()
        {
            feedVersion1.StartFeedVersion1();
            feedVersion3.StartFeedVersion3();

            Console.WriteLine("Feeds Started..");

            await Task.Run(() => Thread.Sleep(Timeout.Infinite));
        }
    }
}