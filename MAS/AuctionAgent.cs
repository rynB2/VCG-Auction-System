using System.Text;
using ActressMas;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace MAS {

    class AuctionAgent : Agent {

        private bool auctionStarted = false; 
        private int totalAgents = 0;    

        private Dictionary<string, List<int>> _lots = new Dictionary<string, List<int>>();  //key: owner of lot (e.g. household 1)
                                                                                            //value: lot as list in format [4, 4, 4] meaning 3 lots of energy (3kwh) for $4/kwh

        private Dictionary<string, List<int>> _bids = new Dictionary<string, List<int>>();  //key: owner of bid (e.g. household 2)
                                                                                            //value: amount of energy (kwh) wanted, price offered per kwh

        private List<KeyValuePair<string, List<int>>> _valuesPaid = new List<KeyValuePair<string, List<int>>>();
        private List<KeyValuePair<string, List<int>>> _secondValuesPaid = new List<KeyValuePair<string, List<int>>>();

        private List<List<KeyValuePair<string, List<int>>>> _secondValuesPaidList = new List<List<KeyValuePair<string, List<int>>>>();

        private List<string> _winnerList = new List<string>(); //key: owner of bid of winner in a lot auction (e.g. household 1)
                                                               //value: amount of energy (kwh) won, price offered per kwh

        private List<List<string>> _2ndWinnerListList = new List<List<string>>();
        private List<string> _secondWinnerList = new List<string>();


        private List<string> buyers = new List<string>();

        private int startBuyers = 0;
        private int noBuyers = 0;
        private int lotSoldValue = 0;
        private int noSellers = 0;

        private int maxBid = 0;
        private bool print = false;
        private bool lotSold = false;
        private bool ended = true;
        string bidWinner;

        public override void Setup() {

        }

        public override void Act(Message message) {
            string senderID = message.Sender; //get the sender's name so we can reply to them
            Experiment.total_messages++;
            Console.WriteLine(message.Format());

            try {
                message.Parse(out string action, out string parameters);

                switch (action) {

                    case "start":
                        totalAgents++;
                        break;

                    case "not_participating":
                        totalAgents--;
                        break;

                    case "not_buying":
                        noBuyers--;
                        totalAgents--;
                        break;

                    case "buyer":
                        buyers.Add(senderID);
                        noBuyers++;
                        break;

                    case "seller":
                        noSellers++;
                        break;

                    case "selling":
                        sortLots(senderID, parameters); //creates lot dictionary in format described above
                        break;


                    case "bid_offer":
                        sortBids(senderID, parameters); //this adds incoming bids to bid dictionary                                           


                        //THIS BLOCK ACTIVATES WHEN ALL BUYERS HAVE BID
                        if (_bids.Count() == noBuyers) {
                            if (_bids.Count == 0) { break; }

                            int kwhSold = 0;

                            int lotRemaining = _lots.First().Value.Count();         //the size of the lot, is reduced when some is sold, so that when lotRemaining == 0 it is fully sold
                            int noBids = _bids.Count();                             //number of bids for this lot
                            int noWinners = _winnerList.Count();                    //amount of winners, so that if amount of winners == number of bids, then everyone has bid

                            List<int> valuesList = new List<int>();                 //create this list at the start of each loop so that its empty

                            foreach (var kvp in _bids) {
                                valuesList.Add(kvp.Value[1]);
                            }

                            List<int> origValues = new List<int>(valuesList);


                            int valueCount = valuesList.Count();

                            bool done = false;

                            List<KeyValuePair<string, List<int>>> _currWinners = new List<KeyValuePair<string, List<int>>>();


                            //FIRST AUCTION
                            //THIS WHILE BLOCK CALCULATES THE ACTUAL WINNERS OF THE AUCTION
                            while (!done) {

                                Dictionary<string, List<int>> _tempBids = new Dictionary<string, List<int>>(_bids);
                                _valuesPaid.Clear();

                                kwhSold = 0;
                                lotRemaining = _lots.First().Value.Count();

                                valuesList.Sort();                                  //sort values lowest to higehst
                                maxBid = valuesList.Last();                         //highest bid is last entry
                                valuesList.Remove(valuesList.Last());               //remove highest bid

                                foreach (var kvp in _tempBids) {                    //gets winner of a bid
                                    if (maxBid == kvp.Value[1] && !(_winnerList.Contains(kvp.Key))) {
                                        bidWinner = kvp.Key;

                                    }
                                }

                                _currWinners.Add(new KeyValuePair<string, List<int>>(bidWinner, _tempBids[bidWinner]));
                                _winnerList.Add(bidWinner);                         //so add them to the winner list

                                foreach (var kvp in _currWinners) {                                    //search through bid dictionary 
                                    if (_winnerList.Contains(kvp.Key)) {                        //only select winners


                                        if (kvp.Value[0] > lotRemaining || kvp.Value[0] == lotRemaining) {              //in the case that a lot is fully sold
                                            List<int> _tempList = new List<int>();
                                            _tempList.Add(lotRemaining);
                                            _tempList.Add(kvp.Value[1]);
                                            _valuesPaid.Add(new KeyValuePair<string, List<int>>(kvp.Key, _tempList));
                                            lotRemaining = 0;

                                        }

                                        else if (kvp.Value[0] < lotRemaining) {         //in the case that a lot is partially sold
                                            _valuesPaid.Add(new KeyValuePair<string, List<int>>(kvp.Key, kvp.Value));
                                            lotRemaining -= kvp.Value[0];
                                        }
                                    }
                                }

                                valueCount = valuesList.Count();

                                if (lotRemaining == 0) {
                                    lotSold = true;
                                    done = true;
                                }
                                if (valueCount == 0) {
                                    done = true;
                                }

                            }


                            List<int> tempHelper = new List<int>();

                            //START SECOND AUCTION
                            //THIS BLOCK IS TO DETERMINE THE SECOND BEST COMBINATION OF BIDS FOR EACH WINNER
                            if (_bids.Count() != _winnerList.Count()) {//IF BIDS ARE SAME AS WINNERLIST, IT MEANS EVERYONE WON SO NO SECOND WINNERS, SO DONT DO THIS PART
                                foreach (var winner in _winnerList) {
                                    List<int> tempValues = new List<int>(origValues);

                                    tempHelper = _bids[winner];
                                    tempValues.Remove(tempHelper[1]); //removes current winner from values

                                    _secondWinnerList.Clear();

                                    done = false;
                                    foreach (var kvp in _bids) {
                                        if (kvp.Key != winner) {   //if participant is not a winner

                                            List<KeyValuePair<string, List<int>>> _currWinners = new List<KeyValuePair<string, List<int>>>();
                                            while (!done) {
                                                _secondValuesPaid.Clear();

                                                Dictionary<string, List<int>> _tempBids2 = new Dictionary<string, List<int>>(_bids);

                                                _tempBids2.Remove(winner);

                                                kwhSold = 0;
                                                lotRemaining = _lots.First().Value.Count();

                                                tempValues.Sort();                                  //sort values lowest to higehst                                                
                                                maxBid = tempValues.Last();                         //highest bid is last entry                                                
                                                tempValues.Remove(tempValues.Last());               //remove highest bid


                                                foreach (var kvp2 in _tempBids2) {                    //search bids                                  //
                                                    if (maxBid == kvp2.Value[1] && !(_secondWinnerList.Contains(kvp2.Key))) {                   //if a bidder's value matches the highest bid
                                                        bidWinner = kvp2.Key;
                                                    }
                                                }

                                                _secondWinnerList.Add(bidWinner);                         //so add them to the winner list
                                                _currWinners.Add(new KeyValuePair<string, List<int>>(bidWinner, _tempBids2[bidWinner]));

                                                foreach (var kvp2 in _currWinners) {                         //search through bid dictionary 
                                                    if (_secondWinnerList.Contains(kvp2.Key)) {            //only select winners

                                                        if (kvp2.Value[0] > lotRemaining || kvp2.Value[0] == lotRemaining) {              //in the case that a lot is fully sold
                                                            List<int> _tempList = new List<int>();
                                                            _tempList.Add(lotRemaining);
                                                            _tempList.Add(kvp2.Value[1]);                                                            
                                                            _secondValuesPaid.Add(new KeyValuePair<string, List<int>>(kvp2.Key, _tempList));
                                                            lotRemaining = 0;
                                                        }

                                                        else if (kvp2.Value[0] < lotRemaining) {         //in the case that a lot is partially sold                                                           
                                                            _secondValuesPaid.Add(new KeyValuePair<string, List<int>>(kvp2.Key, kvp2.Value));
                                                            lotRemaining -= kvp2.Value[0];
                                                        }
                                                    }
                                                }

                                                valueCount = tempValues.Count();
                                                if (lotRemaining == 0) {
                                                    done = true;
                                                }
                                                if (valueCount == 0) {
                                                    done = true;
                                                }
                                            }
                                        }

                                    }
                                    
                                }
                            }

                            //PRICE CALC

                            int firstValue = 0;
                            int secondValue = 0;


                            foreach (var kvp in _bids) {            //tell people without winning bids they aren;t winners
                                if (!_winnerList.Contains(kvp.Key)) {
                                    Send(kvp.Key, "not_winner Sorry, you are not a winner");
                                }
                            }


                            foreach (var kvp in _valuesPaid) {
                                foreach (var kvp2 in _valuesPaid) {     //kvp being entry in bids list, kvp2 being entry in values paid list 
                                    if (kvp2.Key != kvp.Key) {
                                        firstValue += kvp2.Value[0] * kvp2.Value[1];    //adds price of buyers not being considered
                                        
                                    }
                                }

                                if (_bids.Count() == 1) {
                                    firstValue += kvp.Value[0] * kvp.Value[1];
                                }


                                if (_secondValuesPaidList.Count!=0) { //IF BIDS ARE SAMe AS WINNERLIST, IT MEANS EVERYONE WON SO NO SECOND WINNERS, SO DONT DO THIS PART
                                    
                                    var list = new List<KeyValuePair<string, List<int>>>();
                                    list = _secondValuesPaidList.First();
                                    foreach (var value in list) {
                                        secondValue += value.Value[0] * value.Value[1];
                                    }
                                }


                                foreach (var kvp2 in _valuesPaid) {     //kvp being entry in bids list, kvp2 being entry in values paid list 
                                    if (kvp2.Key == kvp.Key) {
                                        if (secondValue != 0) { 
                                            Send(kvp.Key, $"bid_winner Congratulations, you won {kvp2.Value[0]} kWh and paid {(secondValue - firstValue) / kvp2.Value[0]} each"); 
                                        }
                                        if (secondValue == 0) { 
                                            Send(kvp.Key, $"bid_winner Congratulations, you won {kvp2.Value[0]} kWh and paid {(firstValue) / kvp2.Value[0]} each"); 
                                        }
                                        lotSoldValue += (secondValue - firstValue);                                                                                                                                 
                                        firstValue = 0;
                                        secondValue = 0;
                                        
                                        if (_bids.Count() != _winnerList.Count()) {
                                            _secondValuesPaidList.Remove(_secondValuesPaidList.First());
                                        }
                                    }
                                }
                            }


                            if (lotSold) {          //if lot sold (determined by first auction), remove from auction catalogue
                                Send(_lots.First().Key, $"lot_sold You sold {_lots.First().Value.Count()} kWh for a total of {Math.Abs(lotSoldValue)}");
                                _lots.Remove(_lots.First().Key);
                                totalAgents--;
                                noSellers--;
                            }

                            else if (!lotSold) {
                                //reduct lot by amount sold (in first auction)
                                
                                int amtLot = 0;
                                foreach (var kvp2 in _valuesPaid) {
                                    amtLot += kvp2.Value[0];
                                }
                                Send(_lots.First().Key, $"lot_sold You sold {_lots.First().Value.Count() - amtLot} kWh for a total of {Math.Abs(lotSoldValue)}");
                                _lots.First().Value.RemoveRange(0, amtLot);

                            }
                            else {
                                Console.WriteLine("All buyers have satisfied energy requirements but lot still remains");
                            }

                            handleEnd();
                        }
                        break;

                    default:
                        break;
                }

                if (_lots.Count == noSellers){
                    if (_lots.Count != 0 && noBuyers > 0) {
                        if (!print) {
                            Console.Write($"Seller: {_lots.First().Key} Lot: ");
                            for (int i = 0; i < _lots.First().Value.Count; i++) {
                                Console.Write($"{_lots.First().Value[i]}, ");
                            }
                            Console.WriteLine();
                            print = true;
                        }

                        var first = _lots.First();
                        string key = first.Key;
                        var value = first.Value;

                        if (auctionStarted == false) {
                            _bids.Clear();
                            offerLot(key, value);
                            auctionStarted = true; //set back to false once lot is sold
                        }
                    }

                    if (noBuyers == 0 && _lots.Count > 0) {
                        foreach (var kvp in _lots) {
                            Send(kvp.Key, $"unsold_lot Your lot was not fully sold. You have {kvp.Value.Count} kWh remaining.");
                        }
                        Broadcast("auction_concluded");                       
                    }

                    if (_lots.Count == 0 && !ended) {                       
                        Broadcast("auction_concluded");                        
                        ended = false;
                    }


                }
            }

            catch (Exception ex) {
                Console.WriteLine(ex);
            }
        }


        private void sortLots(string sender, string parameters) {   //create lot in format [4, 4, 4]

            var split = parameters.Split(' ');
            int x = int.Parse(split[0]); //how much energy
            int y = int.Parse(split[1]); //price of 1kwh

            List<int> lot = new List<int>();

            for (int i = 0; i < x; i++) {
                lot.Add(y);
            }

            _lots.Add(sender, lot);
        }

        private void sortBids(string sender, string parameters) {

            if (parameters.Contains("not buying")) {
                noBuyers--;
            }
            else {
                var split = parameters.Split(' ');

                int x = int.Parse(split[0]); //how much energy
                int y = int.Parse(split[1]); //price of 1kwh

                List<int> bid = new List<int>();

                bid.Add(x);
                bid.Add(y);

                _bids.Add(sender, bid);
            }
        }

        private void offerLot(string owner, List<int> lot) {
            foreach (var buyer in buyers) {
                Send(buyer, $"lot_sale {owner} is selling {lot.Count} kwh of energy with a reserve price of $ {lot.Max()} /kwh");
            }

        }

        private void handleEnd() {
            _bids.Clear();
            _valuesPaid.Clear();
            _secondValuesPaid.Clear();
            _secondValuesPaidList.Clear();
            _winnerList.Clear();
            _2ndWinnerListList.Clear();
            _secondWinnerList.Clear();

            auctionStarted = false;
            lotSold = false;
            print = false;
            ended = false;

            lotSoldValue = 0;
        }
    }
}
