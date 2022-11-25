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

namespace Slave
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
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
    public partial class MainWindow : Window
    {
        String args = "";
        bool isAddNeeded = false;
        delegate void AppendTextDelegate(Control ctrl, string s);
        AppendTextDelegate _textAppender;
        Socket mainSock;
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

            IPAddress defaultHostAddress = null;
            foreach (IPAddress addr in he.AddressList)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    defaultHostAddress = addr;
                    break;
                }
            }

            if (defaultHostAddress == null)
                defaultHostAddress = IPAddress.Loopback;

            txtAddress.Text = defaultHostAddress.ToString();
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

        void Connect_Click(object sender, RoutedEventArgs e)
        {
            if (mainSock.Connected)
            {
                MessageBox.Show("이미 연결되어 있습니다!");
                return;
            }

            int port;
            if (!int.TryParse(txtPort.Text, out port))
            {
                MessageBox.Show("포트 번호가 잘못 입력되었거나 입력되지 않았습니다.");
                txtPort.Focus();
                txtPort.SelectAll();
                return;
            }
            try { mainSock.Connect(new IPEndPoint(IPAddress.Parse(txtAddress.Text), port)); }
            catch (Exception ex)
            {
                MessageBox.Show("연결에 실패했습니다!\n오류 내용: " + ex.Message, "", MessageBoxButton.OK);
                return;
            }

            AppendText(log, "서버와 연결되었습니다.");

            AsyncObject obj = new AsyncObject(4096);
            obj.WorkingSocket = mainSock;
            mainSock.BeginReceive(obj.Buffer, 0, obj.BufferSize, 0, DataReceived, obj);
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
                MessageBox.Show("서버와의 연결이 끊어졌습니다! 서버 상태를 확인해주세요.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                mainSock.Close();
                mainSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
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

            obj.ClearBuffer();

            obj.WorkingSocket.BeginReceive(obj.Buffer, 0, 4096, 0, DataReceived, obj);
            if (!serialPort1.IsOpen) return;
            serialPort1.Write(msg);
        }
    }
}
