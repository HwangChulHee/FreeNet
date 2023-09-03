using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyTetrisServer
{
    public class TetrisMultiLobby
    {
        Dictionary<string, TetrisLobbyUser> lobby_users; // <lobby_user의 id, TetrisLobbyUser 객체>

        public TetrisMultiLobby()
        {
            lobby_users = new Dictionary<string, TetrisLobbyUser>();
        }

        public void add_lobby_user(TetrisUser tetrisUser)
        {
            TetrisLobbyUser tetrisLobbyUser = new TetrisLobbyUser(tetrisUser, tetrisUser.user_id);
            lobby_users.Add(tetrisUser.user_id, tetrisLobbyUser);

            //Console.WriteLine("\n멀티 로비의 유저 추가");
            show_lobby_user();
        }

        public void remove_lobby_user(TetrisUser tetrisUser)
        {
            lobby_users.Remove(tetrisUser.user_id);

            //Console.WriteLine("\n멀티 로비의 유저 삭제");
            show_lobby_user();
        }

        public void show_lobby_user()
        {
            Console.WriteLine("\n로비 안에 유저");

            foreach (KeyValuePair<string, TetrisLobbyUser> entry in lobby_users)
            {
                Console.WriteLine("로비 안의 유저의 id : " + entry.Key);
            }

            Console.WriteLine();
        }
    }
}
