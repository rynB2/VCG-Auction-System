using ActressMas;
using System;

namespace MAS
{
    public class Program
    {
        private static void Main(string[] args)
        {
            var env = new EnvironmentMas(); //sets up environment

            Console.Write("Enter Number of Houses in system (integer): ");
            int numHouseholds = int.Parse(Console.ReadLine());

            for (int i = 1; i <= numHouseholds; i++)
            {
                var a = new HouseholdAgent();
                env.Add(a, $"Household {i}");
            }

            var auctionManagerAgent = new AuctionAgent();
            env.Add(auctionManagerAgent, "AuctionManager");

            var environmentAgent = new EnvironmentAgent();        
            env.Add(environmentAgent, "environment");

            env.Start();
            Console.ReadKey();
        }
    }
}