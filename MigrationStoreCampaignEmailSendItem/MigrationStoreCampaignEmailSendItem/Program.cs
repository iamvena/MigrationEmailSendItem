using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrationStoreCampaignEmailSendItem
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting...");

            new Migration().UpdateStore();

            Console.WriteLine("Press any key to exit...");

            Console.ReadLine();
        }
    }
}
