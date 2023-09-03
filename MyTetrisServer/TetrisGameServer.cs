using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace MyTetrisServer
{
	using FreeNet;
	using System.Threading;

	class TetrisGameServer
    {
		object operation_lock;
		Queue<CPacket> user_operations;

		// 로직은 하나의 스레드로만 처리한다.
		Thread logic_thread;
		AutoResetEvent loop_event;

		//----------------------------------------------------------------
		// 게임 로직 처리 관련 변수들.
		//----------------------------------------------------------------
		// 게임방을 관리하는 매니저.
		public TetrisRoomManager room_manager { get; private set; }


		//----------------------------------------------------------------

		public TetrisGameServer()
        {
			this.operation_lock = new object();
			this.loop_event = new AutoResetEvent(false);
			this.user_operations = new Queue<CPacket>();

			// 게임 로직 관련.
			this.room_manager = new TetrisRoomManager();

			this.logic_thread = new Thread(gameloop);
			this.logic_thread.Start();
		}

        void gameloop()
		{
			while (true)
			{
				CPacket packet = null;
				lock (this.operation_lock)
				{
					if (this.user_operations.Count > 0)
					{
						packet = this.user_operations.Dequeue();
					}
				}

				if (packet != null)
				{
					// 패킷 처리.
					process_receive(packet);
				}

				// 더이상 처리할 패킷이 없으면 스레드 대기.
				if (this.user_operations.Count <= 0)
				{
					this.loop_event.WaitOne();
				}
			}
		}

		public void enqueue_packet(CPacket packet, TetrisUser user)
		{
			lock (this.operation_lock)
			{
				this.user_operations.Enqueue(packet);
				this.loop_event.Set();
			}
		}

		void process_receive(CPacket msg)
		{
			//todo:
			// user msg filter 체크.

			msg.owner.process_user_operation(msg);
		}


	}
}
