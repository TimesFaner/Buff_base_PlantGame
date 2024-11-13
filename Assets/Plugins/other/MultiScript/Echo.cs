using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class Echo : MonoBehaviour
{
    //UGUI
    public InputField InputFeld;

    public Text text;

    //定义套接字
    private Socket socket;

    //点击连接按钮
    public void Connection()
    {
        //Socket
        socket = new Socket(AddressFamily.InterNetwork,
            SocketType.Stream, ProtocolType.Tcp);
        //Connect
        socket.Connect("127.0.0.1", 8888);
    }

    //点击发送按钮
    public void Send()
    {
        //Send
        var sendStr = InputFeld.text;
        var sendBytes =
            Encoding.Default.GetBytes(sendStr);
        socket.Send(sendBytes);
        //Recv
        var readBuff = new byte[1024];
        var count = socket.Receive(readBuff);
        var recvStr =
            Encoding.Default.GetString(readBuff, 0, count);
        text.text = recvStr;
        //Close
        socket.Close();
    }
}