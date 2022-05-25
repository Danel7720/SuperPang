using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Networking.Transport;
using Unity.Collections;
using NetworkMessages;
using System.Text;

public class Server : MonoBehaviour
{
    public NetworkDriver m_driver;
    public ushort serverPort;
    public NativeList<NetworkConnection> m_Connections;

    void Start()
    {
        //gestor de intercambio de paquetes
        m_driver = NetworkDriver.Create();
        //version de ip a utilizar
        var endpoint = NetworkEndPoint.AnyIpv4;
        //asignar puerto a la version ip
        endpoint.Port = serverPort;
        //Pongo a la escucha el gesto de intercambio de paquetes en red
        if (m_driver.Bind(endpoint) != 0)
        {
            Debug.Log("Fallo al abrir el puerto" + serverPort);
        }
        else
        {
            //si esta abierto me queda a la escucha
            m_driver.Listen();
        }
        //instanciamos las lista de conexiones
        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);

    }

    void Update()
    {
        //completamos las conexiones, es decir, las conexiones que hemos aceptado
        m_driver.ScheduleUpdate().Complete();

       //limpiar conexiones
       for(int i = 0; i< m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
            {
                m_Connections.RemoveAtSwapBack(i);
                i--;
            }
        }
        //Aceptar nuevas conexiones
        NetworkConnection c = m_driver.Accept();
        while (c!=default(NetworkConnection))
        {
            OnConnect(c);
            c = m_driver.Accept();
        }

        //leer mensaje
        DataStreamReader stream;
        for(int i = 0; i< m_Connections.Length; i++)
        {
            Assert.IsTrue(m_Connections[i].IsCreated);
            NetworkEvent.Type cmd;
            cmd = m_driver.PopEventForConnection(m_Connections[i], out stream);
            while(cmd != NetworkEvent.Type.Empty)
            {
                if(cmd == NetworkEvent.Type.Data)
                {
                    OnData(stream, i);
                }
                cmd = m_driver.PopEventForConnection(m_Connections[i], out stream);
            }
        }

    }
    

    private void OnConnect(NetworkConnection c)
    {
        m_Connections.Add(c);
        Debug.Log("Conexion aceptada");
        HandShakeMsg m = new HandShakeMsg();
        m.player.id = c.InternalId.ToString();
        SendToClient(JsonUtility.ToJson(m), c);
    }
    private void OnData(DataStreamReader stream, int numJugador)
    {
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length, Allocator.Temp);
        stream.ReadBytes(bytes);
        string recMsg = Encoding.ASCII.GetString(bytes.ToArray());
        NetworkHeader header = JsonUtility.FromJson<NetworkHeader>(recMsg);

        switch (header.command)
        {
            case Commands.HANDSHAKE:
                HandShakeMsg handShakeRecibido = JsonUtility.FromJson<HandShakeMsg>(recMsg);
                Debug.Log("Se ha conectado " + handShakeRecibido.player.nombre);
                break;
            default:
                Debug.Log("mensaje desconocido");
                break;
        }
    }

    private void SendToClient(string message, NetworkConnection c)
    {
        DataStreamWriter writer;
        m_driver.BeginSend(NetworkPipeline.Null, c, out writer);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message), Allocator.Temp);
        writer.WriteBytes(bytes);
        m_driver.EndSend(writer);
    }
}
