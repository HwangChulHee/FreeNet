using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeNet;

namespace MyTetrisServer
{
    class Program
    {
        static List<TetrisUser> userlist;
        public static TetrisGameServer game_server = new TetrisGameServer();

        static void Main(string[] args)
        {
			CPacketBufferManager.initialize(2000);
			userlist = new List<TetrisUser>();

			CNetworkService service = new CNetworkService();
			// 콜백 매소드 설정.
			service.session_created_callback += on_session_created;
			// 초기화.
			service.initialize();
			service.listen("0.0.0.0", 7979, 100);


			Console.WriteLine("Started!");
			while (true)
			{
				string input = Console.ReadLine();
				//Console.Write(".");
				System.Threading.Thread.Sleep(1000);
			}

			Console.ReadKey();

		}

		static void on_session_created(CUserToken token)
		{
			TetrisUser user = new TetrisUser(token);
			lock (userlist)
			{
				userlist.Add(user);
			}
		}

		// 소켓 연결이 끊어졌을 때 호출되는 메서드
		public static void remove_user(TetrisUser user)
		{
			lock (userlist)
			{
				userlist.Remove(user);

				TetrisGameRoom room = user.tetrisGameRoom;
				if (room != null)
				{
					// 게임 룸이 아니라, 로비에 있을 수도 있기 때문에 그걸 고려해서 삭제해줘야한다. 우선 이렇게해뒀다 나중에 수정 ㄱ
					room.remove_player(user);
                    Console.WriteLine("\n해당 룸에서 유저 삭제");
				}
			}
		}
	}
}
