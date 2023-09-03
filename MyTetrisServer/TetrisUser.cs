using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeNet;

namespace MyTetrisServer
{
    public class TetrisUser : IPeer
    {
        // user가 현재 있는 장소
        public enum UserLocation
        {
            // 어디에도 존재하지 않는 상태
            LOCATION_NO,

            // 테트리스 게임방에 들어가 있는 상태
            LOCATION_GAME_ROOM,

            // 테트리스 멀티플레이 로비에 있는 상태
            LOCATION_MULTI_LOBBY,

        }
        
        CUserToken token;
        public string user_id { get; private set; } // 유저의 id
        public UserLocation userLocation { get; private set;} // user의 상태를 나타내주는 변수
        public TetrisGameRoom tetrisGameRoom { get; private set; } // 멀티플레이를 할 수 있는 테트리스 room
        public TetrisPlayer tetrisPlayer { get; private set; } // 게임방에 들어가있을 때의 객체

        
        //json을 통해 id를 꼭 가져와야함.
        public TetrisUser(CUserToken token)
        {
            this.token = token;
            this.token.set_peer(this);
            userLocation = UserLocation.LOCATION_NO;
            this.user_id = null;
        }

        void IPeer.on_message(Const<byte[]> buffer)
        {
            // 버퍼를 패킷으로 만들고 게임 서버의 큐에 집어넣기. 이후 게임서버를 거쳐서 process_user_operation 실행
            byte[] clone = new byte[40000];
            Array.Copy(buffer.Value, clone, buffer.Value.Length);
            CPacket msg = new CPacket(clone, this);
            Program.game_server.enqueue_packet(msg, this); 
        }

        void IPeer.process_user_operation(CPacket msg)
        {
            PROTOCOL protocol = (PROTOCOL)msg.pop_int32();
            string jsonData = msg.pop_string_many();

            
            if (protocol != PROTOCOL.SEND_TILE_MAP_INFO_REQ && protocol != PROTOCOL.REFRESH_GAME_ROOM_REQ)
            {
                Console.WriteLine("\n\n------------------------");
                Console.WriteLine("수신 데이터");
                Console.WriteLine("user_id " + this.user_id);
                Console.WriteLine("유형 " + protocol);
                Console.WriteLine("json 데이터 " + jsonData);
            }
            
            switch (protocol)
            {
                //////////////////////////////////////////////////////////////////////
                /* 유저 초기화 */
                case PROTOCOL.SEND_USER_ID:
                    UserIdData data = DataForJson<UserIdData>.get_fromJson(jsonData);
                    this.user_id = data.id;
                    break;

                //////////////////////////////////////////////////////////////////////
                /* 방 관련 프로토콜 */

                // 방 생성 요청
                case PROTOCOL.CREATE_GAME_ROOM_REQ:
                    RoomData newRoomData = DataForJson<RoomData>.get_fromJson(jsonData);
                    Program.game_server.room_manager.create_game_room(this, newRoomData); // 방 생성
                    userLocation = UserLocation.LOCATION_GAME_ROOM; // 플레이어 상태 설정                    
                    break;

                // 방 입장 요청
                case PROTOCOL.ENTER_GAME_ROOM_REQ:
                    RoomData oldRoomData = DataForJson<RoomData>.get_fromJson(jsonData);
                    Program.game_server.room_manager.enter_game_room(this, oldRoomData.room_id); // 방 들어가기
                    userLocation = UserLocation.LOCATION_GAME_ROOM; // 플레이어 상태 설정                    
                    break;

                // 방 퇴장 요청
                case PROTOCOL.EXIT_GAME_ROOM_REQ:
                    Program.game_server.room_manager.exit_game_room(this); // 게임 방을 나가고
                    this.tetrisPlayer.exit_room();
                    userLocation = UserLocation.LOCATION_MULTI_LOBBY; // 플레이어 상태 설정     
                    break;

                //////////////////////////////////////////////////////////////////////
                /* 멀티플레이 로비 관련 프로토콜 */

                // 멀티 로비 입장 요청
                case PROTOCOL.ENTER_MULTI_LOBBY_REQ:
                    UserIdData userIdData2 = DataForJson<UserIdData>.get_fromJson(jsonData);
                    Program.game_server.room_manager.enter_multi_lobby(this);                    
                    userLocation = UserLocation.LOCATION_MULTI_LOBBY; // 플레이어 상태 설정                    
                    break;

                // 멀티 로비 퇴장 요청
                case PROTOCOL.EXIT_MULTI_LOBBY_REQ:
                    UserIdData userIdData3 = DataForJson<UserIdData>.get_fromJson(jsonData);
                    Program.game_server.room_manager.exit_multi_lobby(this);
                    userLocation = UserLocation.LOCATION_MULTI_LOBBY; // 플레이어 상태 설정                    
                    break;


                // 게임 방 목록 갱신 요청
                case PROTOCOL.REFRESH_GAME_ROOM_REQ:
                    RoomRefreshReqData roomRefreshReqData = DataForJson<RoomRefreshReqData>.get_fromJson(jsonData);

                    ack_game_room_list(roomRefreshReqData); // 방 목록들에 대한 정보를 보내준다.               
                    break;


                //////////////////////////////////////////////////////////////////////
                /* 테트리스 게임 관련 프로토콜 */
                
                // 게임방 정보에 관한 요청
                case PROTOCOL.OFFER_ROOM_INFO_REQ:
                    UserIdData userIdData4 = DataForJson<UserIdData>.get_fromJson(jsonData);
                    ack_offer_room_info(); // 게임방 정보에 대한 응답
                    break;

                // 팀 변경에 대한 요청
                case PROTOCOL.CHANGE_TEAM_REQ:
                    PlayerData playerData = DataForJson<PlayerData>.get_fromJson(jsonData);
                    this.tetrisGameRoom.change_team_num(playerData);
                    break;

                 // 게임 시작에 대한 요청
                case PROTOCOL.START_GAME_REQ:
                    UserIdData userIdData5 = DataForJson<UserIdData>.get_fromJson(jsonData);
                    this.tetrisGameRoom.ack_start_game(this.tetrisPlayer); // 게임 시작 요청에 대한 응답
                    break;

                // 타일맵 정보에 대한 요청
                case PROTOCOL.SEND_TILE_MAP_INFO_REQ:
                    TileMapInfo tileMapInfo = DataForJson<TileMapInfo>.get_fromJson(jsonData);

                    //자신을 제외한 다른 플레이어들에게 타일맵 정보를 보내준다.
                    this.tetrisPlayer.broadcast_other_player(PROTOCOL.SEND_TILE_MAP_INFO_ACK, tileMapInfo);
                    break;

                // 자신 말고 다른 팀 설정에 대한 요청
                case PROTOCOL.INIT_TARGET_BLOCK_REQ:
                    UserIdData userIdData6 = DataForJson<UserIdData>.get_fromJson(jsonData);
                    this.tetrisPlayer.init_other_team_player();
                    break;

                // 블록 타겟 설정에 대한 요청
                case PROTOCOL.SET_TARGET_BLOCK_REQ:
                    UserIdData userIdData7 = DataForJson<UserIdData>.get_fromJson(jsonData);
                    this.tetrisPlayer.set_target_block();
                    break;

                // 아이템 타겟 설정에 대한 요청
                case PROTOCOL.SET_TARGET_ITEM_REQ:
                    TargetData targetData = DataForJson<TargetData>.get_fromJson(jsonData);
                    this.tetrisPlayer.set_target_item(targetData.target_id);
                    break;

                // 가비지 라인에 대한 요청
                case PROTOCOL.SEND_GARBAGE_LINE_REQ:
                    GarbageLineReqData garbageLineReq = DataForJson<GarbageLineReqData>.get_fromJson(jsonData);
                    this.tetrisPlayer.send_garbage_line(garbageLineReq);
                    break;

                // 게임 종료에 대한 요청
                case PROTOCOL.PLAYER_GAME_OVER_REQ:
                    UserIdData userIdData8 = DataForJson<UserIdData>.get_fromJson(jsonData);
                    this.tetrisPlayer.player_game_over();
                    break;

                // 아이템- 교환에 대한 요청
                case PROTOCOL.ITEM_CHANGE_REQ:
                    TileMapInfo tileMapInfo1 = DataForJson<TileMapInfo>.get_fromJson(jsonData);
                    this.tetrisPlayer.change_tilemap(tileMapInfo1);
                    break;

                // 아이템 - 화면 가리기에 대한 요청
                case PROTOCOL.ITEM_DARK_REQ:
                    UserIdData userIdData9 = DataForJson<UserIdData>.get_fromJson(jsonData);
                    this.tetrisPlayer.ack_item_dark(userIdData9);
                     break;



            }
        }

        // 방 목록에 대한 리스트를 클라이언트에 보여주는 메서드
        public void ack_game_room_list(RoomRefreshReqData roomRefreshReqData)
        {
            // 요청한 페이지, 모드, 현재 페이지, 전체 페이지에 따라서 방 목록을 보여준다.
            RoomRefreshAckData roomRefreshAckData = Program.game_server.room_manager.return_game_room_list(roomRefreshReqData);

            //Console.WriteLine("요청한 방 리스트에 대한 정보 "+ DataForJson<RoomRefreshAckData>.get_toJson(roomRefreshAckData));

            CPacket cPacket = CPacket.create((int)PROTOCOL.REFRESH_GAME_ROOM_ACK);
            cPacket.push_many(DataForJson<RoomRefreshAckData>.get_toJson(roomRefreshAckData));
            this.send(cPacket);
        }

        // 게임방 정보에 대한 응답
        private void ack_offer_room_info()
        {
            RoomData roomData = this.tetrisPlayer.gameRoom.roomData;
            bool isHost = this.tetrisPlayer.isHost;
            int team_num = this.tetrisPlayer.player_team_num;
            int person_num = this.tetrisPlayer.player_person_num;

            List<PlayerData> otherPlayers = new List<PlayerData>();
            // 테트리스 게임방을 순회하며...
            foreach (KeyValuePair<string, TetrisPlayer> entry in this.tetrisGameRoom.players)
            {
                // 자기 자신을 제외한 플레이어의 정보들을 추출한다.
                if (!entry.Value.Equals(tetrisPlayer))
                {
                    TetrisPlayer otherPlayer = entry.Value;
                    otherPlayers.Add(new PlayerData(otherPlayer.player_id, otherPlayer.player_team_num, otherPlayer.player_person_num));  
                }
            }
            OfferRoomData offerRoomData = new OfferRoomData(roomData, isHost, team_num, person_num, otherPlayers);

            //Console.WriteLine("room data 들"+roomData.room_name+" , " + roomData.room_mode + " , " + roomData.password + " , ");
            //Console.WriteLine("보낼 offerRoomData : "+DataForJson<OfferRoomData>.get_toJson(offerRoomData));

            CPacket cPacket = CPacket.create((int)PROTOCOL.OFFER_ROOM_INFO_ACK);
            cPacket.push_many(DataForJson<OfferRoomData>.get_toJson(offerRoomData));
            this.send(cPacket);
        }


        public void send(CPacket msg)
        {
            this.token.send(msg); // 토큰 통해 패킷 보내기            
          
        }

        


        void IPeer.disconnect()
        {
            this.token.socket.Disconnect(false);
        }


        void IPeer.on_removed()
        {
            Program.remove_user(this);
        }


        public void set_player_and_game_room(TetrisPlayer player, TetrisGameRoom tetrisGameRoom)
        {
            this.tetrisPlayer = player;
            this.tetrisGameRoom = tetrisGameRoom;
        }
        

       
    }
}
