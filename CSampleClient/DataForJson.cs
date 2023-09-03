using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CSampleClient
{
    public static class DataForJson<T>
    {
        public static T get_fromJson(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static string get_toJson(T data)
        {
            return JsonConvert.SerializeObject(data);
        }
    }

    public class UserIdData
    {
        public string id;

        public UserIdData(string id)
        {
            this.id = id;
        }
    }

    public class ChatData
    {
        public string id { get; private set; }
        public string msg { get; private set; }

        public ChatData(string id, string msg)
        {
            this.id = id;
            this.msg = msg;
        }
    }

    public class RoomData
    {
        public string room_id { get; private set; }

        public RoomData(string room_id)
        {
            this.room_id = room_id;
        }
    }
}
