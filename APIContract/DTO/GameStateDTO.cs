using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APIContract {

    public class GameStateDTO
    {
        public int gameRound { get; set; }
        public CardDTO[] cards { get; set; }
        public int playerCount { get; set; }
    }






}