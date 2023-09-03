using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
	public enum PROTOCOL : short
	{
		BEGIN = 0,

		/* 게임 시작 시 보내는 요청 (id 저장용) */
		SEND_USER_ID = 1,


		/* 게임 방 관련 요청 */

		// 테트리스 게임방 생성 요청
		CREATE_GAME_ROOM_REQ = 11,

		// 테트리스 게임방 생성 응답
		CREATE_GAME_ROOM_ACK = 12,

		// 테트리스 게임방 입장 요청
		ENTER_GAME_ROOM_REQ = 13,

		// 테트리스 게임방 입장 응답
		ENTER_GAME_ROOM_ACK = 14,



		/* 게임 내 채팅 요청 */

		// 테트리스 게임방 채팅 요청
		CHAT_GAME_ROOM_REQ = 3,

		// 테트리스 게임방 채팅 응답
		CHAT_GAME_ROOM_ACK = 4,

		END
	}
}
