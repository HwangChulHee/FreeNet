using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeNet;


namespace MyTetrisServer
{
    public class TetrisPlayer
    {
        public TetrisUser owner { get; private set; }
        public TetrisGameRoom gameRoom { get; private set; }
        
        public bool isHost { get; private set; }
        public string player_id { get; private set; } // 플레이어의 아이디
        public int player_person_num { get; private set; } // 플레이어의 개인 숫자. (1 ~ 6)
        public int player_team_num; // 플레이어의 팀 숫자. (1 ~ 6)
        public List<TetrisPlayer> other_team_players { get; private set; } // 다른 팀의 플레이어들
        public bool isPlayerEnd; // 게임이 종료되었는가에 대한 여부

        public string target_block_id { get; private set; } // 블록 타겟의 id
        public string target_item_id { get; private set; } // 아이템 타겟의 id


        public TetrisPlayer(TetrisUser owner, string player_id, TetrisGameRoom tetrisGameRoom)
        {
            this.owner = owner;
            this.player_id = player_id;
            this.gameRoom = tetrisGameRoom;
            this.other_team_players = new List<TetrisPlayer>();
            
         
            
            // 현재 방의 인원 수 +1로 팀 숫자와 개인 숫자를 정해줌.
            int temp_team_num = 1;
            if (gameRoom.players.Count() == 0)
            {
                this.player_team_num = temp_team_num;
                

            } else
            {
                foreach (KeyValuePair<string, TetrisPlayer> entry in tetrisGameRoom.players)
                {
                    //Console.WriteLine("\ntemp num : "+temp_team_num+" , entry num : "+entry.Value.player_team_num);
                    while (true)
                    {
                        if (temp_team_num != entry.Value.player_team_num)
                        {
                            this.player_team_num = temp_team_num;
                            break;

                        }
                        temp_team_num++;
                        if (6 < temp_team_num)
                        {
                            temp_team_num = 1;
                        }
                    }
                    
                }
            }

            int temp_person_num = 1;
            if (gameRoom.players.Count() == 0)
            {
                this.player_person_num = temp_person_num;
            }
            else
            {
                foreach (KeyValuePair<string, TetrisPlayer> entry in tetrisGameRoom.players)
                {
                    //Console.WriteLine("\ntemp num : " + temp_person_num + " , entry num : " + entry.Value.player_person_num);
                    while (true)
                    {
                        if (temp_person_num != entry.Value.player_person_num)
                        {
                            this.player_person_num = temp_person_num;
                            break;

                        }
                        temp_person_num++;
                        if (6 < temp_person_num)
                        {
                            temp_person_num = 1;
                        }
                    }

                }
            }

        }

        public void set_host()
        {
            isHost = true;
        }

        // 자기 자신한테 패킷을 보내는 경우
        public void send_own<T>(PROTOCOL protocol_id, T data)
        {
            CPacket cPacket = CPacket.create((int)protocol_id);
            cPacket.push_many(DataForJson<T>.get_toJson(data));
            this.owner.send(cPacket);
        }

        // 다른 사람한테 패킷을 보내는 경우
        public void send_other_player<T>(TetrisPlayer otherPlayer ,PROTOCOL protocol_id, T data)
        {
            CPacket cPacket = CPacket.create((int)protocol_id);
            cPacket.push_many(DataForJson<T>.get_toJson(data));
            otherPlayer.owner.send(cPacket);
        }

        // 자신을 제외한 방의 모든 인원에게 패킷을 보내는 경우
        public void broadcast_other_player<T>(PROTOCOL protocol_id, T data)
        {
            foreach (KeyValuePair<string, TetrisPlayer> entry in this.gameRoom.players)
            {
                // 자기 자신이 아닌 경우에 정보를 보내준다.
                if (!entry.Value.Equals(this))
                {
                    CPacket cPacket = CPacket.create((int)protocol_id);
                    cPacket.push_many(DataForJson<T>.get_toJson(data));
                    entry.Value.owner.send(cPacket);
                }
            }
        }

        // 방의 모든 인원에게 패킷을 보내는 경우
        public void broadcast_all_player<T>(PROTOCOL protocol_id, T data)
        {
            foreach (KeyValuePair<string, TetrisPlayer> entry in this.gameRoom.players)
            {
                CPacket cPacket = CPacket.create((int)protocol_id);
                cPacket.push_many(DataForJson<T>.get_toJson(data));
                entry.Value.owner.send(cPacket);
            }
        }




        // 방을 들어가는 메서드.
        public void enter_room()
        {
            this.send_own(PROTOCOL.ENTER_GAME_ROOM_ACK, new UserIdData("approve"));
        }
        
        // 방을 나가는 메서드. 클라이언트에 대한 응답.
        public void exit_room()
        {
            // 게임룸에서 자신을 제외한 모든 이들에게 나간다고 응답을 해준다.
            this.broadcast_other_player(PROTOCOL.EXIT_GAME_ROOM_ACK, new UserIdData(this.player_id));
            
            this.gameRoom.roomData.room_people--;
            this.gameRoom.remove_player(this.owner); // tetrisGameRoom에서 본인 삭제


            // 게임룸에 있는 멤버들이 가지고 있는 다른 플레이어 중 나를 없애준다.
            foreach (KeyValuePair<string, TetrisPlayer> entry in this.gameRoom.players)
            {
                List<TetrisPlayer> player_otherPlayer_list = entry.Value.other_team_players;
                
                for (int i = 0; i < player_otherPlayer_list.Count; i++)
                {
                    if (this == player_otherPlayer_list[i])
                    {
                        player_otherPlayer_list.RemoveAt(i);
                    }
                }

            }



            if (this.gameRoom.isStarted)
            {
               this.game_over(); // 게임 오버를 확인하고 타겟(블록, 아이템)을 설정해주는 메서드
            }

        }


        // 다른 팀 설정
        public void init_other_team_player()
        {
            foreach (KeyValuePair<string, TetrisPlayer> entry in this.gameRoom.players)
            {
                // 자신과 team_num이 다르다면
                if (entry.Value.player_team_num != this.player_team_num)
                {
                    // 해당 플레이어를 넣어준다.
                    other_team_players.Add(entry.Value);
                }
            }

            set_target_block();
            set_target_item(this.target_block_id);
        }

        // 블록 타겟 설정
        public void set_target_block()
        {
            while(other_team_players.Count != 0)
            {
                Random random = new Random();
                int randomNumber = random.Next(0, other_team_players.Count);

                // 해당 플레이어가 게임오버가 아닐 시에만
                if (!other_team_players[randomNumber].isPlayerEnd)
                {
                    this.target_block_id = other_team_players[randomNumber].player_id;
                    this.send_own(PROTOCOL.SET_TARGET_BLOCK_ACK, new TargetData(this.target_block_id));
                    Console.WriteLine("\n\n 타겟 변경 성공! player_id : "+this.player_id+" , targer_block_id : "+this.target_block_id);
                    break;
                }
            }            
        }

        // 아이템 타겟 설정
        public void set_target_item(string target_item_id)
        {
            this.target_item_id = target_item_id;
            this.send_own(PROTOCOL.SET_TARGET_ITEM_ACK, new TargetData(target_item_id));
        }

        public void set_target_item_random()
        {
            while (other_team_players.Count != 0)
            {
                Random random = new Random();
                int randomNumber = random.Next(0, other_team_players.Count);

                // 해당 플레이어가 게임오버가 아닐 시에만
                if (!other_team_players[randomNumber].isPlayerEnd)
                {
                    this.target_item_id = other_team_players[randomNumber].player_id;
                    this.send_own(PROTOCOL.SET_TARGET_ITEM_ACK, new TargetData(this.target_item_id));
                    break;
                }
            }

        }


        // 가비지 라인 보내주기
        public void send_garbage_line(GarbageLineReqData reqData)
        {
            // 가비지 라인에서 한줄을 제외할 블록의 y 좌표
            Random random = new Random();
            int randomNumber = random.Next(-5, 5);


            List<int[]> vector_list = new List<int[]>();

            // 행의 개수는 가비지 라인의 수다.
            for (int i = -10; i < -10+reqData.count_garbage_line; i++)
            {
                for (int j = -5; j < 5; j++)
                {
                    if (j != randomNumber)
                    {
                        int[] vector = new int[3];
                        vector[0] = j;
                        vector[1] = i;
                        vector[2] = 0;

                        vector_list.Add(vector);
                    } 
                }
            }

            GarbageLineAckData garbageLineAckData = new GarbageLineAckData(
                    reqData.target_id, 
                    reqData.count_garbage_line, 
                    vector_list);

            TetrisPlayer target_player = this.gameRoom.players[reqData.target_id];
            this.send_other_player(target_player, PROTOCOL.SEND_GARBAGE_LINE_ACK, garbageLineAckData);
        }


        // 플레이어 게임 오버시 처리
        public void player_game_over()
        {
            Console.WriteLine("\n게임종료 메서드 호출");
            this.isPlayerEnd = true;
            this.broadcast_other_player(PROTOCOL.PLAYER_GAME_OVER_ACK, new UserIdData(this.player_id));

            
            // 게임 오버를 확인하고 타겟(블록, 아이템)을 설정해주는 메서드
            this.game_over();
        }

        // 게임 오버를 확인하고 타겟(블록, 아이템)을 설정해주는 메서드
        public void game_over()
        {
            // 게임 오버가 되었다면 해당 프로토콜을 보내주고, 타겟 설정을 굳이 해주진 않는다.
            // return_isGamv
            int win_team_num = this.gameRoom.return_gameOverTeam();
            if (win_team_num != -1)
            {
                foreach (KeyValuePair<string, TetrisPlayer> entry in this.gameRoom.players)
                {
                    //Console.WriteLine(
                    //    "player_id : "+entry.Value.player_id+
                    //    ", player_team_num : "+entry.Value.player_team_num+
                    //    ", win_team_num : "+win_team_num
                    //    );
                    
                    // 이긴 팀과 자신의 팀을 비교해서 승/패 여부를 결정해서 데이터를 보내준다.
                    GameOverData otherGameOverData = new GameOverData(entry.Value.player_team_num == win_team_num);

                    CPacket cPacket = CPacket.create((int)PROTOCOL.GAME_OVER_ACK);
                    cPacket.push_many(DataForJson<GameOverData>.get_toJson(otherGameOverData));
                    entry.Value.owner.send(cPacket);
                }

                this.gameRoom.isStarted = false;
                return;
            }

            // 해당 플레이어가 게임 종료되면 다시 타겟을 설정해준다.
            foreach (KeyValuePair<string, TetrisPlayer> entry in this.gameRoom.players)
            {
                // 블록의 타겟과 아이템의 타겟이 게임 오버된 사람이라면, 다시 타겟을 설정해준다.

                if (entry.Value.target_block_id == this.player_id)
                {
                    entry.Value.set_target_block();
                }

                if (entry.Value.target_item_id == this.player_id)
                {
                    entry.Value.set_target_item_random();
                }

            }
        }

        public void change_tilemap(TileMapInfo tileMapInfo)
        {
            Console.WriteLine("target_item_id : "+tileMapInfo.tilemap_user_id);

            TetrisPlayer target_player = this.gameRoom.players[tileMapInfo.tilemap_user_id];
            this.send_other_player(target_player, PROTOCOL.ITEM_CHANGE_ACK, tileMapInfo);
        }

        public void ack_item_dark(UserIdData userIdData)
        {
            this.gameRoom.players[userIdData.id].send_own(PROTOCOL.ITEM_DARK_ACK, new UserIdData("시야 가리기"));
        }
    }

}
