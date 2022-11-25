using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Master
{
    public class AsyncObject
    {
        public byte[] Buffer;
        public Socket WorkingSocket;
        public readonly int BufferSize;
        public AsyncObject(int bufferSize)
        {
            BufferSize = bufferSize;
            Buffer = new byte[BufferSize];
        }

        public void ClearBuffer()
        {
            Array.Clear(Buffer, 0, BufferSize);
        }
    }
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        String args = "";
        bool isAddNeeded = false;
        bool isServerStarted = false;
        delegate void AppendTextDelegate(Control ctrl, string s);
        AppendTextDelegate _textAppender;
        Socket mainSock;
        IPAddress thisAddress;
        DispatcherTimer timer = new DispatcherTimer();  
        SerialPort serialPort1 = new SerialPort();
        public MainWindow()
        {
            InitializeComponent();
            foreach (var item in SerialPort.GetPortNames())
            {
                comport.Items.Add(item);
            }
            timer.IsEnabled = true;
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += new EventHandler(Timer_Tick);
            timer.Start();
            mainSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            _textAppender = new AppendTextDelegate(AppendText);

            IPHostEntry he = Dns.GetHostEntry(Dns.GetHostName());

            foreach (IPAddress addr in he.AddressList)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    thisAddress = addr;
                    break;
                }
            }

            if (thisAddress == null)
                thisAddress = IPAddress.Loopback;

            txtAddress.Text = thisAddress.ToString();
        }

        private void comport_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        private void portconnect_Click(object sender, RoutedEventArgs e)
        {
            if (comport.Text == "") return;
            try
            {
                if (serialPort1.IsOpen)
                {
                    serialPort1.Close();
                }
                else
                {
                    serialPort1.PortName = comport.SelectedItem.ToString();
                    serialPort1.BaudRate = 9600;
                    serialPort1.DataBits = 8;
                    serialPort1.StopBits = StopBits.One;
                    serialPort1.Parity = Parity.None;
                    serialPort1.Open();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "알림", MessageBoxButton.OK);
            }
            portconnect.Content = serialPort1.IsOpen ? "연결해제" : "연결하기";
            comport.IsEnabled = !serialPort1.IsOpen;
        }

        private void Send_COM_Click(string txt)
        {
            if (!serialPort1.IsOpen) return;
            serialPort1.Write(txt);
        }

        private void ComboBoxItem_Selected(object sender, RoutedEventArgs e)
        {

        }

        private void comport_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            comport.Items.Clear();
            
        }

        public void Timer_Tick(object sender, EventArgs e)
        {
            if (isAddNeeded)
            {
                addLogs(args);
                args = "";
                isAddNeeded = false;
            }
        }

        void AppendText(Control ctrl, string s)
        {
            if (!ctrl.Dispatcher.CheckAccess())
            {
                ctrl.Dispatcher.Invoke(_textAppender, ctrl, s);
            }
            else
            {
                ctrl.SetValue(TextBlock.TextProperty, ctrl.GetValue(TextBlock.TextProperty) + Environment.NewLine + s);
                ctrl.SetValue(ContentControl.ContentProperty, ctrl.GetValue(ContentControl.ContentProperty) + Environment.NewLine + s);
            }
            args = s;
            isAddNeeded = true;
        }
        public void addLogs(String str)
        {
            log.AppendText(str + Environment.NewLine);
            log.Select(log.Text.Length, 0);
            log.ScrollToEnd();
        }

        void Start_Click(object sender, RoutedEventArgs e)
        {
            int port;
            if (!int.TryParse(txtPort.Text, out port))
            {
                MessageBox.Show("포트 번호가 잘못 입력되었거나 입력되지 않았습니다.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                txtPort.Focus();
                txtPort.SelectAll();
                return;
            }

            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, port);
            mainSock.Bind(serverEP);
            mainSock.Listen(10);

            mainSock.BeginAccept(AcceptCallback, null);
            AppendText(log, string.Format("서버가 시작되었습니다. ipv4: {0}:{1}", txtAddress.Text, txtPort.Text));
            isServerStarted = true;
        }

        List<Socket> connectedClients = new List<Socket>();
        void AcceptCallback(IAsyncResult ar)
        {
            Socket client = mainSock.EndAccept(ar);

            mainSock.BeginAccept(AcceptCallback, null);

            AsyncObject obj = new AsyncObject(4096);
            obj.WorkingSocket = client;

            connectedClients.Add(client);

            AppendText(log, string.Format("클라이언트 (@ {0})가 연결되었습니다.", client.RemoteEndPoint));

            client.BeginReceive(obj.Buffer, 0, 4096, 0, DataReceived, obj);
        }

        void DataReceived(IAsyncResult ar)
        {
            AsyncObject obj = (AsyncObject)ar.AsyncState;
            int received;
            try
            {
                received = obj.WorkingSocket.EndReceive(ar);
            }
            catch (Exception ex)
            {
                AppendText(log, "클라이언트 " + obj.WorkingSocket.RemoteEndPoint + " 에서 연결해제 되었습니다.");
                return;
            }

            if (received <= 0)
            {
                obj.WorkingSocket.Close();
                return;
            }

            string text = Encoding.UTF8.GetString(obj.Buffer);
            string[] tokens = text.Split('\x01');
            string ip = tokens[0];
            string msg = tokens[1];
            msg = msg.Trim('\0');
            string resulttxt;
            resulttxt = msg;
            AppendText(log, string.Format("[받음]{0}: {1}", ip, resulttxt));

            for (int i = connectedClients.Count - 1; i >= 0; i--)
            {
                Socket socket = connectedClients[i];
                if (socket != obj.WorkingSocket)
                {
                    try { socket.Send(obj.Buffer); }
                    catch
                    {
                        try { socket.Dispose(); } catch { }
                        connectedClients.RemoveAt(i);
                    }
                }
            }

            obj.ClearBuffer();

            obj.WorkingSocket.BeginReceive(obj.Buffer, 0, 4096, 0, DataReceived, obj);
        }

        void Send_Click(string txtTTS)
        {
            if (!mainSock.IsBound)
            {
                MessageBox.Show("서버가 실행되고 있지 않습니다!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string tts = txtTTS.Trim();
            string ttse;
            ttse = tts;
            if (string.IsNullOrEmpty(tts))
            {
                MessageBox.Show("텍스트가 입력되지 않았습니다!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            byte[] bDts = Encoding.UTF8.GetBytes(thisAddress.ToString() + '\x01' + ttse);

            for (int i = connectedClients.Count - 1; i >= 0; i--)
            {
                Socket socket = connectedClients[i];
                try { socket.Send(bDts); }
                catch
                {
                    try { socket.Dispose(); } catch { }
                    connectedClients.RemoveAt(i);
                }
            }

            AppendText(log, string.Format("[보냄]{0}: {1}", thisAddress.ToString(), tts));
        }


        private void Red_Click(object sender, RoutedEventArgs e)
        {
            Send_Click("RED");
            Send_COM_Click("RED");
        }

        private void Green_Click(object sender, RoutedEventArgs e)
        {
            Send_Click("GREEN");
            Send_COM_Click("GREEN");
        }
    }
}
