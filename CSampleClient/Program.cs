using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using FreeNet;

namespace CSampleClient
{
	using GameServer;

	class Program
	{
		static List<IPeer> game_servers = new List<IPeer>();

		public enum STATE
		{

			CHOICE_INIT,

			CHOICE_ROOM_CREATE,

			CHOICE_ROOM_ENTER,

			CHOICE_CHAT,

		}


		static string user_id = null;

		static void Main(string[] args)
		{
			CPacketBufferManager.initialize(2000);
			// CNetworkService객체는 메시지의 비동기 송,수신 처리를 수행한다.
			// 메시지 송,수신은 서버, 클라이언트 모두 동일한 로직으로 처리될 수 있으므로
			// CNetworkService객체를 생성하여 Connector객체에 넘겨준다.
			CNetworkService service = new CNetworkService();

			// endpoint정보를 갖고있는 Connector생성. 만들어둔 NetworkService객체를 넣어준다.
			CConnector connector = new CConnector(service);
			// 접속 성공시 호출될 콜백 매소드 지정.
			connector.connected_callback += on_connected_gameserver;
			IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7979);
			connector.connect(endpoint);
			//System.Threading.Thread.Sleep(10);

			STATE state = STATE.CHOICE_INIT;
			
			
			while (true)
			{

                if (user_id == null)
                {
					continue;
                }

                switch (state)
                {
                    case STATE.CHOICE_INIT:
						Console.Write("방 생성 : 1, 방 입장 : 2, 나가기 : q >");
						break;
                    case STATE.CHOICE_ROOM_CREATE:
						Console.Write("(방 생성) 방 아이디 입력 (나가기 : q) :  > ");
						break;
                    case STATE.CHOICE_ROOM_ENTER:
						Console.Write("(방 입장) 방 아이디 입력 (나가기 : q) :  > ");
						break;
                    case STATE.CHOICE_CHAT:
						Console.Write("채팅 (나가기 : q) > ");
						break;
                    default:
                        break;
                }

                string line = Console.ReadLine();

                if (state == STATE.CHOICE_INIT)
                {
					if (line == "1")
					{
						state = STATE.CHOICE_ROOM_CREATE;

					}
					else if (line == "2")
					{
						state = STATE.CHOICE_ROOM_ENTER;
					}

                }
                else if(state == STATE.CHOICE_ROOM_CREATE)
                {
					CPacket msg = CPacket.create((short)PROTOCOL.CREATE_GAME_ROOM_REQ);
					string json = DataForJson<UserIdData>.get_toJson(new UserIdData(line));
					msg.push(json);
					game_servers[0].send(msg);

					
					state = STATE.CHOICE_CHAT;

				}
				else if (state == STATE.CHOICE_ROOM_ENTER)
				{
					CPacket msg = CPacket.create((short)PROTOCOL.ENTER_GAME_ROOM_REQ);
					string json = DataForJson<RoomData>.get_toJson(new RoomData(line));
					msg.push(json);
					game_servers[0].send(msg);

					
					state = STATE.CHOICE_CHAT;
				}
				else if (state == STATE.CHOICE_CHAT)
				{
					CPacket msg = CPacket.create((short)PROTOCOL.CHAT_GAME_ROOM_REQ);
					string json = DataForJson<ChatData>.get_toJson(new ChatData(user_id, line));
					msg.push(json);
					game_servers[0].send(msg);

				}

                if (line == "q")
                {
					break;
                }

			}

			((CRemoteServerPeer)game_servers[0]).token.disconnect();

			//System.Threading.Thread.Sleep(1000 * 20);
			Console.ReadKey();
		}

		/// <summary>
		/// 접속 성공시 호출될 콜백 매소드.
		/// </summary>
		/// <param name="server_token"></param>
		static void on_connected_gameserver(CUserToken server_token)
		{
			lock (game_servers)
			{
				IPeer server = new CRemoteServerPeer(server_token);
				game_servers.Add(server);
				Console.WriteLine("Connected!");
			}
			
			Console.Write("user id 입력 >");
			user_id = Console.ReadLine();

			CPacket msg = CPacket.create((short)PROTOCOL.SEND_USER_ID);
			string json = DataForJson<UserIdData>.get_toJson(new UserIdData(user_id));
			msg.push(json);

            Console.WriteLine("id 전송 완료 : "+json);

			game_servers[0].send(msg);
		}
	}
}
