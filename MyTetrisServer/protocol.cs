using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyTetrisServer
{
	public enum PROTOCOL : int
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

		// 테트리스 게임방 퇴장 요청
		EXIT_GAME_ROOM_REQ = 15,

		// 테트리스 게임방 퇴장 응답
		EXIT_GAME_ROOM_ACK = 16,

		// 게임방 호스트 양도 응답 (서버에서만 쓰임)
		ASSIGN_HOST_ACK = 17,

		// 게임방 입장시 정보 제공 요청
		OFFER_ROOM_INFO_REQ = 18,

		// 게임방 입장시 정보 제공 응답
		OFFER_ROOM_INFO_ACK = 19,

		// 다른 플레이가 게임방 입장시 정보 제공에 대한 응답(서버에서만 쓰임)
		ENTER_PLAYER_ACK = 110,

		// 팀 변경 요청
		CHANGE_TEAM_REQ = 111,

		// 팀 변경 응답
		CHANGE_TEAM_ACK = 112,

		
		
		/* 게임 중 관련 응답 */
		
		// 게임 시작 요청
		START_GAME_REQ = 21,

		// 게임 시작 응답
		START_GAME_ACK = 22,

		// 타일맵 정보 요청
		SEND_TILE_MAP_INFO_REQ = 23,

		// 타일맵 정보 요청
		SEND_TILE_MAP_INFO_ACK = 24,

		// 블록 타겟 요청 (초기화) , 다른 팀 확정
		INIT_TARGET_BLOCK_REQ = 25,

		// 블록 타겟 요청
		SET_TARGET_BLOCK_REQ = 26,

		// 블록 타겟 응답
		SET_TARGET_BLOCK_ACK = 27,

		// 아이템 타겟 요청
		SET_TARGET_ITEM_REQ = 28,

		// 아이템 타겟 응답
		SET_TARGET_ITEM_ACK = 29,

		// 가비지 라인 전송 요청
		SEND_GARBAGE_LINE_REQ = 211,

		// 가비지 라인 전송 응답
		SEND_GARBAGE_LINE_ACK = 212,

		// 플레이어 게임 오버 요청
		PLAYER_GAME_OVER_REQ = 213,

		// 플레이어 게임 오버 응답
		PLAYER_GAME_OVER_ACK = 214,

		// 게임 오버 요청
		GAME_OVER_REQ = 215,

		// 게임 오버 응답
		GAME_OVER_ACK = 216,

		// 아이템 - 교환 요청
		ITEM_CHANGE_REQ = 217,

		// 아이템 - 교환 응답
		ITEM_CHANGE_ACK = 218,

		// 아이템 - 시야가리기 요청
		ITEM_DARK_REQ = 219,

		// 아이템 - 시야가리기 응답
		ITEM_DARK_ACK = 220,




		/* 멀티 로비 관련 요청 */

		// 멀티 로비 입장 요청
		ENTER_MULTI_LOBBY_REQ = 31,

		// 멀티 로비 입장 응답
		ENTER_MULTI_LOBBY_ACK = 32,

		// 멀티 로비 퇴장 요청
		EXIT_MULTI_LOBBY_REQ = 33,

		// 멀티 로비 퇴장 응답
		EXIT_MULTI_LOBBY_ACK = 34,

		// 테트리스 게임방 목록 갱신 요청
		REFRESH_GAME_ROOM_REQ = 35,

		// 테트리스 게임방 목록 갱신 응답
		REFRESH_GAME_ROOM_ACK = 36,



		/* 게임 내 채팅 요청 */

		// 테트리스 게임방 채팅 요청
		CHAT_GAME_ROOM_REQ = 51,

		// 테트리스 게임방 채팅 응답
		CHAT_GAME_ROOM_ACK = 52,

		END
	}
}
