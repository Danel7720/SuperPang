using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Networking.Transport;
using Unity.Collections;
using System.Text;
using NetworkMessages;
using TMPro;
using UnityEngine.UI;
using NetworkObject;

public class NetworkClient : MonoBehaviour
{
    public NetworkDriver m_driver;
    public NetworkConnection m_Connection;
    public string serverIP;
    public ushort serverPort;
    public InputField inputNombre;
    private bool empezar = false;
    public string idPlayer;

    public object InputNombre { get; private set; }

    public void Conectar()
    {
        m_driver = NetworkDriver.Create();
        m_Connection = default(NetworkConnection);
        var endpoint = NetworkEndPoint.Parse(serverIP, serverPort);
        m_Connection = m_driver.Connect(endpoint);

        inputNombre.gameObject.SetActive(false);
        GameObject.Find("Button").SetActive(false);
        GameObject.Find("Text").GetComponent<Text>().text = "Esperando...";
        empezar = true;
    }

    void Update()
    {
        if (!empezar)
        {
            return;
        }

        m_driver.ScheduleUpdate().Complete();
        if (!m_Connection.IsCreated)
        {
            return;
        }
        //se a establecido bien la conexion, podemos mandar mensajes
        DataStreamReader stream;
        //strem recibe los mensajes
        NetworkEvent.Type cmd = m_Connection.PopEvent(m_driver, out stream);
        while (cmd != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                OnConnect();
            }else if (cmd == NetworkEvent.Type.Data)
            {
                OnData(stream);
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {

            }
            //pasar al siguiente mensaje
            cmd = m_Connection.PopEvent(m_driver, out stream);
        }
    }
    private void OnConnect()
    {
        Debug.Log("Contectado correctamente");
    }

    private void OnData(DataStreamReader stream)
    {
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length, Allocator.Temp);
        stream.ReadBytes(bytes);
        string recMsg = Encoding.ASCII.GetString(bytes.ToArray());
        NetworkHeader header = JsonUtility.FromJson<NetworkHeader>(recMsg);

        switch (header.command)
        {
            case Commands.HANDSHAKE:
                HandShakeMsg handShakeRecibido = JsonUtility.FromJson<HandShakeMsg>(recMsg);
                HandShakeMsg handShakeEnviar = new HandShakeMsg();
                idPlayer = handShakeRecibido.player.id;
                handShakeEnviar.player.nombre = inputNombre.text;
                SendToServer(JsonUtility.ToJson(handShakeEnviar));
                break;
            default:
                Debug.Log("Mensaje desconocido");
                break;
        }
    }
    private void SendToServer(string message)
    {
        DataStreamWriter writer;
        m_driver.BeginSend(NetworkPipeline.Null, m_Connection, out writer);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message), Allocator.Temp);
        writer.WriteBytes(bytes);
        m_driver.EndSend(writer);
    }
}
