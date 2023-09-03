using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeNet;

namespace MyTetrisServer
{
    class TetrisRoomManager
    {
        Dictionary<string, TetrisGameRoom> tetris_game_room; // <TetrisGameRoom의 id, TetrisGameRoom> 로 구성됨.
        TetrisMultiLobby tetris_multi_lobby; // 멀티플레이를 위한 로비.
        
        List<TetrisGameRoom> multi_game_room; // 멀티방에서 보이는 게임 룸을 위한 리스트

        public TetrisRoomManager()
        {
            tetris_game_room = new Dictionary<string, TetrisGameRoom>();
            tetris_multi_lobby = new TetrisMultiLobby();
            multi_game_room = new List<TetrisGameRoom>();
        }

        // 테트리스 멀티 로비 입장 요청

        public void enter_multi_lobby(TetrisUser tetrisUser)
        {
            // 로비 입장
            tetris_multi_lobby.add_lobby_user(tetrisUser);

            // 게임룸 퇴장
            if (tetrisUser.tetrisGameRoom != null)
            {
                exit_game_room(tetrisUser);
            }
            
        }

        // 테트리스 멀티 로비 퇴장 요청

        public void exit_multi_lobby(TetrisUser tetrisUser)
        {
            tetris_multi_lobby.remove_lobby_user(tetrisUser); /// 로비 퇴장
        }




        // 테트리스 게임방 생성 요청, 멀티로비에서 해당 유저를 제거한다
        public void create_game_room(TetrisUser user, RoomData roomData)
        {
            
            TetrisGameRoom tetrisGameRoom = new TetrisGameRoom(roomData); // 게임 방 생성

            tetrisGameRoom.add_player(user); // 해당 게임방에 플레이어로 user를 넣어줌.
            this.tetris_game_room.Add(tetrisGameRoom.roomData.room_id, tetrisGameRoom); // 테트리스 게임방 유저에 user를 추가
            this.multi_game_room.Add(tetrisGameRoom);

            exit_multi_lobby(user); // multi_lobby에서 유저를 삭제하는 로직

            // 멀티 로비에 있는 유저들에게 방을 생성하는 
            
            show_game_room();
        }

        // 
        public RoomRefreshAckData return_game_room_list(RoomRefreshReqData roomRefreshReqData)
        {
            RoomRefreshAckData roomRefreshAckData = new RoomRefreshAckData();

            int req_room_mode = roomRefreshReqData.mode; // 0-전체 방, 1-노템전, 2-아이템전
            int enable_room_num = 0; // 전체 방의 개수. 시작한 방은 제외한다.
            for (int i = 0; i < multi_game_room.Count; i++)
            {
                if (!multi_game_room[i].isStarted)
                {
                    
                    if (req_room_mode == 0)
                    {
                        // 전체 방 요청
                        enable_room_num++;
                    } 
                    else if(req_room_mode == 1)
                    {
                        // 노템전 요청
                        if (multi_game_room[i].roomData.room_mode == 0)
                        {
                            //노템전일 때만 카운트
                            enable_room_num++;

                        }
                    }
                    else if (req_room_mode == 2)
                    {
                        // 아이템전 요청
                        if (multi_game_room[i].roomData.room_mode == 1)
                        {
                            //아이템전일 때만 카운트
                            enable_room_num++;

                        }
                    }
                }
            }
             
            int entry_page = 1;
            if (enable_room_num != 0)
            {
                entry_page = Convert.ToInt32(Math.Ceiling((double)enable_room_num / 6)); // 전체 페이지
            }
            
            int request_page = roomRefreshReqData.request_page; // 요청 페이지

            
            // 요청 페이지가 전체 페이지가 크다면, 타당함에 false를 주고 바로 리턴한다.
            if (entry_page < request_page)
            {
                //roomRefreshAckData.isValid = false;
                //return roomRefreshAckData;
                request_page--; // 요청페이지를 감소시키고 로직 진행.
            }

            int start_index = (request_page - 1) * 6;
            int last_index = start_index + 6;

            // last_index가 전체 방의 개수보다 크다면, 전체 방의 개수를 last_index로 설정해준다.
            if (enable_room_num < last_index)
            {
                last_index = enable_room_num;
            }

            //Console.WriteLine("start_index : "+start_index+" , last_index : "+last_index);
            //Console.WriteLine("multi_game_room_count : "+enable_room_num);


            for (int i = start_index; i < last_index; i++)
            {
                
                TetrisGameRoom tetrisGameRoom = multi_game_room[i];
                roomRefreshAckData.roomDatas.Add(tetrisGameRoom.roomData);

                //Console.WriteLine("\n저장한 방들에 대한 정보");
                //Console.WriteLine(DataForJson<RoomData>.get_toJson(tetrisGameRoom.roomData)+ "\n");
            }

            roomRefreshAckData.current_page = request_page;
            roomRefreshAckData.entry_page = entry_page;
            roomRefreshAckData.isValid = true;

            return roomRefreshAckData;
        }
       

        // 테트리스 게임방 입장 요청, 멀티로비에서 해당 유저를 제거한다
        public void enter_game_room(TetrisUser user, string room_id)
        {

            if (tetris_game_room.ContainsKey(room_id))
            {
                string room_state = tetris_game_room[room_id].add_player(user);
                
                if (room_state.Equals("full"))
                {
                    CPacket cPacket = CPacket.create((int)PROTOCOL.ENTER_GAME_ROOM_ACK);
                    cPacket.push_many(DataForJson<UserIdData>.get_toJson(new UserIdData("full")));
                    user.send(cPacket);

                } else if(room_state.Equals("started"))
                {
                    CPacket cPacket = CPacket.create((int)PROTOCOL.ENTER_GAME_ROOM_ACK);
                    cPacket.push_many(DataForJson<UserIdData>.get_toJson(new UserIdData("started")));
                    user.send(cPacket);

                } else if(room_state.Equals("approve"))
                {    
                    exit_multi_lobby(user); // multi_lobby에서 유저를 삭제하는 로직
                }
                

            } else
            {
                CPacket cPacket = CPacket.create((int)PROTOCOL.ENTER_GAME_ROOM_ACK);
                cPacket.push_many(DataForJson<UserIdData>.get_toJson(new UserIdData("reject")));
                user.send(cPacket);
            }
            
        }

        // 테트리스 게임방 퇴장 요청
        public void exit_game_room(TetrisUser user)
        {
            TetrisGameRoom room_of_user = user.tetrisGameRoom;
            if (room_of_user != null)
            {
                room_of_user.remove_player(user);

            }
            else
            {
                Console.WriteLine("\n해당 방이 없습니다!");
            }

        }

        // 테트리스 게임방 삭제 요청
        public void remove_game_room (string room_id)
        {
           
            if (this.tetris_game_room.ContainsKey(room_id))
            {
                TetrisGameRoom tetrisGameRoom;
                this.tetris_game_room.TryGetValue(room_id, out tetrisGameRoom);
                this.multi_game_room.Remove(tetrisGameRoom);

                if (this.tetris_game_room.Remove(room_id))
                {    
                    Console.WriteLine("\n테트리스 게임방 삭제 성공 : " + room_id);
                }

            }

            show_game_room();           
        }

        public void show_game_room()
        {
            Console.WriteLine("\n게임 방 목록 : ");
            foreach (KeyValuePair<string, TetrisGameRoom> entry in tetris_game_room)
            {
                Console.WriteLine("게임 방 : " + entry.Key);
            }
        }

     
    }
}
