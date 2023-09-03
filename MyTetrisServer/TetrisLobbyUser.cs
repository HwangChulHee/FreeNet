using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyTetrisServer
{
    public class TetrisLobbyUser
    {
        public TetrisUser owner { get; private set; }
        public string lobby_user_id { get; private set; }

        public TetrisLobbyUser(TetrisUser owner, string lobby_user_id)
        {
            this.owner = owner;
            this.lobby_user_id = lobby_user_id;
        }
    }
}
