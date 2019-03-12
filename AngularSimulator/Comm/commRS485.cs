using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AngularSimulator.Comm.ASProtocol;

namespace AngularSimulator.Comm
{

    /// <summary>
    /// RS485 통신 클래스
    /// </summary>
    class CommRS485
    {
        /// <summary>
        /// 시리얼 통신 객체 생성
        /// </summary>
        private SerialPort serialPort = new SerialPort();

        /// <summary>
        /// 데이터 수신처리 델리게이트 선언
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="type"></param>
        //public delegate void OnDataReceived(Object packet, int type);
        
        /// <summary>
        /// 수신 콜백 객체 생성
        /// </summary>
        private ASProtocol prototolHandler = null;

        /// <summary>
        /// 시리얼 포트 이름
        /// </summary>
        private String portName = "COM1";

        public string PortName { get => portName; set => portName = value; }
        public bool ResponseFlag { get => responseFlag; set => responseFlag = value; }

        private bool responseFlag;

        /// <summary>
        /// RS485 통신 클래스 생성
        /// </summary>
        /// <param name="portName"></param>
        /// <param name="rcvCallback"></param>
        public CommRS485(String portName, OnParsingCompleted rcvCallback)
        {
            initComm(9600);
            PortName = portName;
            prototolHandler = new ASProtocol(rcvCallback);
        }
        public CommRS485(String portName, OnParsingCompleted rcvCallback, int baudrate)
        {
            initComm(baudrate);
            PortName = portName;
            prototolHandler = new ASProtocol(rcvCallback);
        }
        /// <summary>
        /// 통신 초기화 메소드
        /// </summary>
        private void initComm(int baudrate)
        {
            // fixed config values
            //serialPort.BaudRate = 115200;
            serialPort.BaudRate = baudrate;
            serialPort.DataBits = 8;
            serialPort.Parity = System.IO.Ports.Parity.None;
            serialPort.StopBits = System.IO.Ports.StopBits.One;
            serialPort.Handshake = System.IO.Ports.Handshake.None;
            serialPort.DataReceived += SerialPort_DataReceived; ;
            serialPort.PinChanged += SerialPort_PinChanged;
        }

        /// <summary>
        /// 시리얼 통신 핀 이벤트 처리
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SerialPort_PinChanged(object sender, SerialPinChangedEventArgs e)
        {
            Console.WriteLine(e);
        }

        /// <summary>
        /// 시리얼 데이터 수신 이벤트 처리
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            ResponseFlag = true;    // 무언가 수신이 되었으면 응답 받은걸로..
            String rcvData = serialPort.ReadLine();// serialPort.ReadExisting();
            if( prototolHandler.CheckPacket(rcvData) == false)
            {
               
            }

        }

        /// <summary>
        /// 통신 포트를 연결한다.
        /// </summary>
        /// <returns></returns>
        public bool Open()
        {
            if (!serialPort.IsOpen)
            {  
                try
                {
                    serialPort.PortName = PortName;
                    serialPort.Open();
                }
                catch (Exception e)
                {
                    return false;
                }
            }
            return serialPort.IsOpen;
        }

        /// <summary>
        /// 통신 포트를 종료한다.
        /// </summary>
        public void Close()
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }

        /// <summary>
        /// 통신 가능한 포트를 가져온다
        /// </summary>
        /// <returns></returns>
        static public String[] GetPortNames()
        {
            return SerialPort.GetPortNames();
        }

        public void sensorCheck(int id)
        {
            string cmd = String.Format("{0}CID{1}{2}", (char)0x02, id.ToString("D2"), (char)0x03);
            prototolHandler.LastSendPkt = cmd;
            send(Encoding.UTF8.GetBytes(cmd));
        }
        public void readSensor(int id)
        {
            string cmd = String.Format("{0}MID{1}{2}", (char)0x02, id.ToString("D2"), (char)0x03);
            prototolHandler.LastSendPkt = cmd;
            send(Encoding.UTF8.GetBytes(cmd));
        }
        public void ReSend()
        {
            send(Encoding.UTF8.GetBytes(prototolHandler.LastSendPkt));
        }


        public void send(String pkt)
        {   
            send(Encoding.UTF8.GetBytes(pkt));
                
        }
        private void send(byte[] buffer)
        {
            if (serialPort.IsOpen)
            {
                ResponseFlag = false;
                serialPort.Write(buffer, 0, buffer.Length);
            }

        }
        public bool isOpen()
        {
            return serialPort.IsOpen;
        }

    }
}
