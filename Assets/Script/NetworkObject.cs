using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetworkObject
{
    [System.Serializable]

    public class NetworkObject
    {
        public string id;
    }
    [System.Serializable]

    public class NetworkPlayer: NetworkObject
    {
        public Vector3 posjugador;
        public string nombre;
    }
}

namespace NetworkMessages
{
    public enum Commands
    {
        HANDSHAKE,
        READY
    }

    [System.Serializable]

    public class NetworkHeader
    {
        public Commands command;
    }

    [System.Serializable]

    public class HandShakeMsg : NetworkHeader
    {
        public NetworkObject.NetworkPlayer player;
        public HandShakeMsg()
        {
            command = Commands.HANDSHAKE;
            player = new NetworkObject.NetworkPlayer();
        }
    }

    [System.Serializable]

    public class ReadyMsg  : NetworkHeader
    {
        public List<NetworkObject.NetworkPlayer> player;
        public ReadyMsg()
        {
            command = Commands.READY;
            player = new List<NetworkObject.NetworkPlayer>();
        }
    }
}
