using System;
using System.Collections.Generic;
using System.Text;
using ActressMas;
using System.Linq;

namespace MAS
{
    class HouseholdAgent : Agent
    {

        public int demand = 0;
        public int generation = 0;
        public int priceToBuyFromUtility = 0;
        public int priceToSellToUtility = 0;

        public int energySurplus = 0;

        public bool announcedSold = false;
        public bool announcedBuying = false;

        public bool seller = false;
        public bool buyer = false;
        public bool participating = true;

        public int valuation = 0;

        public int cost = 0;
        public int moneyBalance = 0;
        public int energyBalance = 0;

        public bool notSaid = false;

        
        private static Random rand = new Random();

        public override void Setup()
        {
            Send("environment", "start");
            Send("AuctionManager", "start");
            
        }

        public override void Act(Message message)
        {
            string senderID = message.Sender; //get the sender's name so we can reply to them
            Experiment.total_messages++;

            if (message.Sender == "AuctionManager") {
                Console.WriteLine(message.Format());
            }

            string offerer;
            int amtOffered;
            int price;

            try {

                if (!participating) {
                    if(!(buyer || seller)) {
                        Stop();
                    }
                }

                message.Parse(out string action, out string parameters);

                switch (action) {

                    case "inform":
                        var values = parameters.Split(' ');

                        demand = int.Parse(values[0]);
                        generation = int.Parse(values[1]);
                        priceToBuyFromUtility = int.Parse(values[2]);
                        priceToSellToUtility = int.Parse(values[3]);

                        energySurplus = BuyOrSell(demand, generation);
                        energyBalance = energySurplus;
                        break;

                    case "lot_sale": 
                        values = parameters.Split(' ');

                        offerer = values[0] + " " + values[1];
                        amtOffered = int.Parse(values[4]);
                        price = int.Parse(values[14]);

                        if (price > valuation) {    //if the lot is too expensive
                            Send("AuctionManager", "bid_offer not buying");
                            break;
                        }

                        else if (buyer == true && energySurplus == 0 && !notSaid) {
                            Send("AuctionManager", "bid_offer not buying");
                            notSaid = true;

                            break;
                        }

                        else if (participating) {
                            Send("AuctionManager", $"bid_offer {Math.Min(energySurplus, amtOffered)} {valuation}");
                        }

                        break;

                    case "bid_winner": 
                        values = parameters.Split();
                        moneyBalance -= (int.Parse(values[3]) * int.Parse(values[7]));
                        energySurplus -= int.Parse(values[3]);
                        if (energySurplus == 0) {
                            participating = false;
                        }
                        break;

                    case "lot_sold": 
                        values = parameters.Split();
                        energySurplus += int.Parse(values[2]);
                        moneyBalance += int.Parse(values[8]);                       
                        break;

                    case "auction_concluded":                        
                        if (buyer == true) {
                            if (energySurplus != 0) {
                                moneyBalance -= energySurplus * priceToBuyFromUtility;
                                energySurplus = 0;
                            }
                            
                            Experiment.total_utility += Math.Abs(Math.Abs(moneyBalance) - (priceToBuyFromUtility * energyBalance));
                            Experiment.buyer_profit += Math.Abs(Math.Abs(moneyBalance) - (priceToBuyFromUtility * energyBalance));
                            Stop();
                            if (Environment.NoAgents == 2) {
                                Console.WriteLine($"Utility: {Experiment.total_utility}, Messages sent: {Experiment.total_messages}, Buyer savings: {Experiment.buyer_profit}, Seller profit: {Experiment.seller_profit}");
                            }
                            Stop();
                        }
                        if (seller == true) {
                            if (energySurplus != 0) {
                                moneyBalance += Math.Abs(energySurplus * priceToSellToUtility);
                                energySurplus = 0;
                            }
                            Experiment.total_utility += moneyBalance - (priceToSellToUtility * energyBalance);
                            Experiment.seller_profit += moneyBalance - (priceToSellToUtility * energyBalance);
                            Stop();
                            if (Environment.NoAgents == 2) {

                                Console.WriteLine($"Utility: {Experiment.total_utility}, Messages sent: {Experiment.total_messages}, Buyer savings: {Experiment.buyer_profit}, Seller profit: {Experiment.seller_profit}");
                            }
                            Stop();
                        }
                        
                        break;

                    default:
                        break;
                }

                if (announcedSold == false && seller == true) {
                    Send("AuctionManager", $"selling {Math.Abs(energySurplus)} {valuation}");
                    announcedSold = true;
                }

            }
            catch (Exception ex) {
                Console.WriteLine(ex);
            }
        }

        public int BuyOrSell(int d, int g) {
            
            energySurplus = d - g;

            if (energySurplus > 0) {
                valuation = priceToBuyFromUtility - rand.Next(0, 3);
                Send("AuctionManager", "buyer");
                Console.WriteLine($"{ Name} requires {energySurplus} energy and will pay a maximum of {valuation}"); //max price to buy is 1 less than the utility company price            
                buyer = true;
                return energySurplus;
            }

            if (energySurplus < 0) {
                valuation = priceToSellToUtility + rand.Next(0, 3);
                Send("AuctionManager", "seller");
                Console.WriteLine($"{ Name} has {Math.Abs(energySurplus)} energy to sell for a minimum of {valuation}");  //min price to sell is 1 over utility company                 
                seller = true;                
                return Math.Abs(energySurplus);
            }

            if (demand != 0 && energySurplus == 0) {
                Send("AuctionManager", "not_participating I am no longer participating");
                participating = false;
                return energySurplus;
            }

            return -1;
        }
    }
}
