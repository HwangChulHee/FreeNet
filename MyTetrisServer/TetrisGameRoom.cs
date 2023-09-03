using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeNet;

namespace MyTetrisServer
{
    public class TetrisGameRoom
    {
        public RoomData roomData { get; private set; } // 룸에 관한 정보가 들어있음. 방 id, 방 이름, 방 모드, 비밀방 여부, 비밀번호
        public Dictionary<string, TetrisPlayer> players; // <player의 id, player 객체>
        public bool isStarted; // 게임 시작 여부
        public bool isGameOver { get; private set; } // 게임 종료 여부


        public TetrisGameRoom(RoomData roomData)
        {
            this.roomData = roomData;
            players = new Dictionary<string, TetrisPlayer>();
            isStarted = false;
            Console.WriteLine("\n테트리스 게임방 생성 : " + this.roomData.room_id);
        }


        // 플레이어를 추가해주는 메서드
        public string add_player(TetrisUser user)
        {
            if (this.isStarted)
            {
                return "started";
            }

            if (players.Count < 6)
            {
                TetrisPlayer player = new TetrisPlayer(user, user.user_id, this);
                if (players.Count == 0)
                {
                    player.set_host(); // plyaer의 수가 0이면 방장.
                } else
                {
                    player.enter_room(); // 방장이 아니면 플레이어의 방 입장 처리를 해준다.
                }
                players.Add(player.player_id, player);
                user.set_player_and_game_room(player, this); // TetrisUser 객체에 플레이어와 방 정보 추가.
                roomData.room_people++;

                send_playerData_to_other(player); //플레이어가 추가되면 자신을 제외한 다른 플레이어들에게 자신의 정보를 보내준다.


                Console.WriteLine("\n플레이어가 방안에 입장 성공. room id : "+roomData.room_id+" , player_id : "+player.player_id);
                show_room_players();
                return "approve";
            } else
            {
                return "full";
            }
            
        }
        // 플레이어를 삭제하는 메서드
        public void remove_player(TetrisUser user)
        {
            //room에서 해당 플레이어 삭제
            TetrisPlayer tetrisPlayer = user.tetrisPlayer;
            players.Remove(tetrisPlayer.player_id);
            

            //플레이어 삭제 후, 방의 인원이 한 명도 없으면 
            if (players.Count == 0)
            {
                // 이 방을 삭제한다.

                Program.game_server.room_manager.remove_game_room(this.roomData.room_id);

                // 여기서도 패킷을 보내줘야한다.
            }

            // 호스트이고 게임이 시작되거나 시작되지 않았거나 상관없다.
            if (tetrisPlayer.isHost)
            {
                // 해당 방에 있는 다른 플레이어에게 패킷을 보내준다.
                foreach (KeyValuePair<string, TetrisPlayer> entry in players)
                {
                    // 자기 자신이 아닌 경우에 방 생성 종료라는 메시지를 보내준다.
                    if (!entry.Value.Equals(tetrisPlayer))
                    {
                        CPacket cPacket = CPacket.create((int)PROTOCOL.ASSIGN_HOST_ACK);
                        cPacket.push_many(DataForJson<UserIdData>.get_toJson(new UserIdData("z")));
                        entry.Value.owner.send(cPacket);
                        entry.Value.set_host();
                        break; // 랜덤으로 한명한테 방장을 양도하고 끝낸다.
                    }
                }
            }

        }
        // 플레이어 자신의 정보를 방에 있는 다른 사람들에게 보내준다.
        public void send_playerData_to_other(TetrisPlayer tetrisPlayer)
        {
            //Console.WriteLine("send_playerData_to_other 호출 확인");
            PlayerData playerData = new PlayerData(tetrisPlayer.player_id, tetrisPlayer.player_team_num, tetrisPlayer.player_person_num);

            Console.WriteLine(
                "player id : "+tetrisPlayer.player_id+
                ", player_team_num : "+tetrisPlayer.player_team_num+
                ", player_person_num : "+tetrisPlayer.player_person_num
                );

            // 방에 있는 다른 사람들에게 나의 정보를 보내준다. 
            tetrisPlayer.broadcast_other_player(PROTOCOL.ENTER_PLAYER_ACK, playerData);
        }


        // 자신의 팀 정보를 바꾸고 변경 정보를 다른 플레이어들에게 보내는 메서드
        public void change_team_num(PlayerData playerData)
        {
            Console.WriteLine("\n change_team_num 메서드 호출");

            // 자신의 팀 정보를 바꾼다.

            players[playerData.player_id].player_team_num = playerData.player_team_num;
            players[playerData.player_id].broadcast_other_player(PROTOCOL.CHANGE_TEAM_ACK, playerData);
        }


        // 게임이 시작했다는 정보를 다른 플레이어들에게 알리는 메서드
        public void ack_start_game(TetrisPlayer tetrisPlayer)
        {
            // 게임이 시작가능한지에 따라서..
            if (!isEnable_game_start())
            {
                // 게임 시작이 불가능하면
                tetrisPlayer.send_own(PROTOCOL.START_GAME_ACK, new UserIdData("start_reject_by_own"));
                return;
            } else
            {
                // 게임 시작이 가능하면
                tetrisPlayer.send_own(PROTOCOL.START_GAME_ACK, new UserIdData("start_approve_by_own"));
            }

            // 게임방이 시작되었다고 설정한다.
            this.isStarted = true; 
            // 다른 사람들에게 게임이 시작했다고 보내준다.
            tetrisPlayer.broadcast_other_player(PROTOCOL.START_GAME_ACK, new UserIdData("start_approve_by_other"));

            foreach (KeyValuePair<string, TetrisPlayer> entry in players)
            {
                entry.Value.isPlayerEnd = false;
            }

        }
           
        // 이 게임방이 시작할 수 있는지에 대한 결정을 내려주는 메서드다.
        private bool isEnable_game_start()
        {
            

            // 당연히 혼자 시작할 수 없다.
            if (players.Count == 1)
            {
                Console.WriteLine("\n방 시작 불가능. 혼자임");
                return false;
            }


            int index = 1;
            int player1_team = -999;

            // 한 명이라도 다르다면 true..
            foreach (KeyValuePair<string, TetrisPlayer> entry in players)
            {
                if (index == 1)
                {
                    player1_team = entry.Value.player_team_num;
                }


                
                if (player1_team != entry.Value.player_team_num)
                {
                    Console.WriteLine("\n방 시작 가능");
                    return true;
                    
                }
                index++;
            }

            Console.WriteLine("\n방 시작 불가능. 팀이 다 똑같음");
            return false;
        }
        
        // 이 게임이 끝났는지 판별해주는 메서드. 승리팀을 반환해준다.
        public int return_gameOverTeam()
        {
            // 팀들을 비교해가며 이 게임방이 종료되었는지를 검사해준다.

            
            /* 1. 우선 존재하는 팀들을 구해준다. */
            List<int> existing_team_list = new List<int>();
            
            // 방 안에 있는 모든 플레이어들을 순회하며
            foreach (KeyValuePair<string, TetrisPlayer> entry in this.players)
            {
                // 플레이어의 팀 숫자를 가져오고
                int player_team_num = entry.Value.player_team_num;
                if (!existing_team_list.Contains(player_team_num))
                {
                    // 팀 리스트에 추가하지 않았다면 팀의 숫자를 택해준다.
                    existing_team_list.Add(player_team_num);
                }
            }

            /* 2. 팀의 게임오버 유무를 저장하는 해쉬맵을 생성해준다.*/
            Dictionary<int, bool> isGameOver_team_list = new Dictionary<int, bool>();
            for (int i = 0; i < existing_team_list.Count; i++)
            {
                // 우선 모두 게임종료되지 않았다고 초기화를 해준다.
                isGameOver_team_list.Add(existing_team_list[i], false);
            }


            /* 3. 지금 존재하는 팀들에 대하여.. 한 팀을 택하고, */
            for (int i = 0; i < existing_team_list.Count; i++)
            {
                int select_team_num = existing_team_list[i]; // 존재하는 팀 중, 한 팀을 택한다.
                bool is_select_team_gameOver = true; // 해당 팀의 게임 종료 여부를 true값으로 해준다.

                // 해당 팀의 멤버들이 모두 게임 오버되었는지 검사해준다.
                foreach (KeyValuePair<string, TetrisPlayer> entry in this.players)
                {
                    // 선택한 팀과 같은 플레이어들을 가져온다.
                    if (select_team_num == entry.Value.player_team_num)
                    {
                        TetrisPlayer tetrisPlayer = entry.Value;
                        // 해당 플레이어가 게임종료되지 않았다면 팀의 게임오버 유무를 false값으로 바꿔준다.
                        // 이 부분 좀 헷갈릴 수 있음.
                        if (!tetrisPlayer.isPlayerEnd)
                        {
                            is_select_team_gameOver = false;
                        }
                        
                    }
                }

                // 해당 팀이 게임 종료가 되었다면 
                if (is_select_team_gameOver)
                {
                    isGameOver_team_list[select_team_num] = true; //해당 팀의 게임 종료를 true값으로 바꿔준다.
                }
            }


            /* 4. 만약 게임 종료가 된 팀이 한 팀만 남았다면 게임이 종료되었다고 해준다.*/
            int not_game_over_team_num = 0;
            int win_team = -1;
            foreach (KeyValuePair<int, bool> entry in isGameOver_team_list)
            {
                // 게임 종료가 되지 않은 팀이 있다면
                if (!entry.Value)
                {
                    not_game_over_team_num++;
                    win_team = entry.Key; // 팀의 숫자
                }
            }



            // 게임 
            if (not_game_over_team_num == 1)
            {
                return win_team; // 게임이 종료되었음.

            } else
            {
                return -1; // 게임이 종료되지 않음.
            }

            
            
        }
        
        public void show_room_players()
        {
            Console.WriteLine("\n방 안의 플레이어");
            
            foreach (KeyValuePair<string, TetrisPlayer> entry in players)
            {
                Console.WriteLine("플레이어 id : "+entry.Key);
            }

            Console.WriteLine();
        }


    }
}
