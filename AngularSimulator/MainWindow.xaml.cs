using AngularSimulator.Comm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Diagnostics;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Media; //음악 사용
using System.Windows.Media.Animation; // Animation 사용

namespace AngularSimulator
{
    /// <summary>
    /// DesignWindow.xaml에 대한 상호 작용 논리
    /// </summary>  
    /// 
    public partial class DesignWindow : Window
    {
        Stopwatch stopwatch = new Stopwatch();
        private int limit = 28;
        private List<Port> ports = new List<Port>();
        private List<ASPacket> logDatas = new List<ASPacket>();
        private CommRS485 comm = null;
        private CommRS485 DOutComm = null;
        private volatile bool isMusicOn = false;
        private long musiclength = 600;
        private long nextTimeout;
        private const int FERRIS = 8;
        private int movingdelay = 200;
        private int minimumreaddelay = 150;
        private int angleCorrectValue = 0;
        private double lastcorrectangle = 0;
        private double[] lastAngleList = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        private double[] ferrisAngleList = { 0, 0, 0, 0, 0, 0, 0, 0, 0 }; // 0=wheel angle, 나머지는 관람차 각도

        public DesignWindow()
        {
            stopwatch.Start();
            InitializeComponent();


            tbSendCycle.PreviewKeyDown += TbSendCycle_PreviewKeyDown;
            tbSensorID.PreviewKeyDown += TbSendCycle_PreviewKeyDown;
            tbAngleCorrection.PreviewKeyDown += TbSendCycle_PreviewKeyDown;

            resetPorts();

            initLabels();

            updateAngleAni(ferrisAngleList[0]);



            //DrawWheel drawWheel = new DrawWheel();
            //mainCanvas.Children.Add(drawWheel.GeometryDrawingExample());
        }


        /// <summary>
        /// 텍스트 박스 키보드 입력 이벤트 메소드
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TbSendCycle_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!(((Key.D0 <= e.Key) && (e.Key <= Key.D9)) || ((Key.NumPad0 <= e.Key) && (e.Key <= Key.NumPad9)) || e.Key == Key.Back))
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// 시리얼 포트 초기화
        /// </summary>
        public void resetPorts()
        {
            String[] portNames = CommRS485.GetPortNames();

            foreach (String portname in portNames)
            {
                ports.Add(new Port { PortNumber = Int32.Parse(portname.Replace("COM", "")), PortName = portname });
            }

            cbComPort.ItemsSource = ports;
            cbComPort.DisplayMemberPath = "PortName";

            if (cbComPort.Items.Count > 0)
            {
                cbComPort.SelectedIndex = 0;
            }

            cbComPort2.ItemsSource = ports;
            cbComPort2.DisplayMemberPath = "PortName";

            if (cbComPort2.Items.Count > 0)
            {
                cbComPort2.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// 시작 버튼 클릭 이벤트 처리 메소드
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            Port port = cbComPort.SelectedItem as Port;
            comm = new CommRS485(port.PortName, OnDataReceived);

            if (comm.Open())
            {
                //통신이 연결되면 명령어 전송 스레드를 시작한다.
                Thread sendCmdThread = new Thread(new ThreadStart(SendCommandLoop));
                sendCmdThread.Start();
            }
            else
            {
                if (comm != null)
                {
                    comm.Close();
                    comm = null;
                }

                MessageBox.Show("ERROR 1 - 통신연결 실패");
            }
        }

        /// <summary>
        /// 종료 버튼 클릭 이벤트 처리 메소드
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (comm != null)
            {
                comm.Close();
                comm = null;
            }
        }
        private void btnDinOpen_Click(object sender, RoutedEventArgs e)
        {
            if (comm != null && comm.isOpen())
            {
                if (comm != null)
                {
                    comm.Close();
                    comm = null;
                }

                btnDinOpen.Content = "Open";
            }
            else
            {
                Port dInport = cbComPort.SelectedItem as Port;
                comm = new CommRS485(dInport.PortName, OnDataReceived);

                if (comm.Open())
                {
                    //통신이 연결되면 명령어 전송 스레드를 시작한다.
                    Thread sendCmdThread = new Thread(new ThreadStart(SendCommandLoop));
                    sendCmdThread.Start();

                    btnDinOpen.Content = "Close";
                }
                else
                {
                    if (comm != null)
                    {
                        comm.Close();
                        comm = null;
                    }
                    btnDinOpen.Content = "Open";

                    MessageBox.Show("ERROR 1 : D-In 통신연결 실패. 포트를 확인하세요.");
                }
            }





        }

        /// <summary>
        /// D-Out 버튼 클릭 이벤트 처리 메소드
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDoutOpen_Click(object sender, RoutedEventArgs e)
        {
            if (DOutComm != null && DOutComm.isOpen())
            {
                if (DOutComm != null)
                {
                    DOutComm.Close();
                    DOutComm = null;
                }
                btnDoutOpen.Content = "Open";
            }
            else
            {
                Port dOutport = cbComPort2.SelectedItem as Port;
                DOutComm = new CommRS485(dOutport.PortName, OnOutDataReceived, 115200);

                if (DOutComm.Open())
                {
                    btnDoutOpen.Content = "Close";
                }
                else
                {
                    if (DOutComm != null)
                    {
                        DOutComm.Close();
                        DOutComm = null;
                        btnDoutOpen.Content = "Open";
                    }
                    MessageBox.Show("ERROR 2 : D-Out 통신연결 실패. 포트를 확인하세요.");
                }

            }

        }


        /// <summary>
        /// 데이터 수신 이벤트 처리 메소드
        /// </summary>
        /// <param name="packet"></param>
        public void OnDataReceived(Object parsered, Object rawpkt, bool isCompleted)
        {
            UIUpdateDelegate updater = delegate
            {
                if (isCompleted == false)
                {
                    //재전송
                    comm.ReSend();

                    return;
                }
                if (DOutComm != null && DOutComm.isOpen())
                {
                    DOutComm.send(rawpkt as String);
                }

                var rcvPkt = parsered as string[];
                try
                {
                    var id = rcvPkt[0];
                    var nId = int.Parse(id.Substring(1, 2));

                    ASPacket pkt = null;
                    if (id.Contains("C"))
                    {
                        pkt = new ASPacket() { Id = nId, TimeStamp = System.DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss") };
                        logDatas.Insert(0,new ASPacket() { Id = nId, TimeStamp = System.DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss") });
                    }
                    else
                    {
                        var x = double.Parse(rcvPkt[1]);
                        var y = double.Parse(rcvPkt[2]);
                        int wheellimitx_ = int.Parse(tbWheelAlarmX_.Text);
                        int limitx_ = int.Parse(tbSeatAlarmX_.Text);
                        int wheellimitx = int.Parse(tbWheelAlarmX.Text);
                        int limitx = int.Parse(tbSeatAlarmX.Text);

                        //          int ylimit = int.Parse(tbSeatAlarmY.Text);

                        if (nId == 1)
                        {

                            if ( wheellimitx_ <=x &&x<=wheellimitx)
                            {

                                long milliseconds = stopwatch.ElapsedMilliseconds;
                                if (milliseconds >= nextTimeout)
                                {
                                    isMusicOn = false;
                                }
                                if (isMusicOn == false)
                                {
                                    Thread t = new Thread(new ThreadStart(Danger_Music));
                                    isMusicOn = true;
                                    nextTimeout = milliseconds + musiclength;
                                    t.Start();

                                }


                                Danger_Message(-1, 0);
                            }
                            else
                            {
                                MyMessage.IsOpen = false;
                            }
                        }
                        else
                        {
                            if (limitx_ <=x && x<= limitx)
                            {
                                long milliseconds = stopwatch.ElapsedMilliseconds;
                                if (milliseconds >= nextTimeout)
                                {
                                    isMusicOn = false;
                                }
                                if (isMusicOn == false)
                                {
                                    Thread t = new Thread(new ThreadStart(Danger_Music));
                                    isMusicOn = true;
                                    nextTimeout = milliseconds + musiclength;
                                    t.Start();

                                }
                                Danger_Message(nId, 1);
                            }
                            else
                            { 
                                MyMessage2.IsOpen = false;

                            }


                        }
                        pkt = new ASPacket() { TimeStamp = System.DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss"), Id = nId, X = x, Y = y, Status = "DATA" };
                        logDatas.Insert(0,new ASPacket() { TimeStamp = System.DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss"), Id = nId, X = x, Y = y, Status = "DATA" });

                        updateLabelContens(nId, x.ToString());


                        // 관람차 및 휠의 각도를 리스트에 저장해 놓고
                        if (nId > 0 && nId <= FERRIS + 1)
                        {
                            lastAngleList[nId - 1] = ferrisAngleList[nId - 1];
                            ferrisAngleList[nId - 1] = x;
                        }

                        // 휠의 각도가 들어올 때 전체 각도를 적용한다.
                        if (nId == 1)
                        {
                            updateAngleAni(x);
                        }

                    }
                    if (logDatas.Count >= limit)
                    {
                        logDatas.RemoveAt(limit-1);
                    }
                    //ICollectionView view = CollectionViewSource.GetDefaultView(logDatas);
                    //view.Refresh();
                    Thread update2 = new Thread(delegate ()
                    {
                        LogUpdate(pkt);
                    });
                    update2.Start();
                }
                catch (Exception e)
                {

                }

            };
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, updater);
        }
        private void LogUpdate(ASPacket pk)
        {
            UIUpdateDelegate updater = delegate
            {
                ICollectionView view = CollectionViewSource.GetDefaultView(logDatas);
                view.Refresh();
            };
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, updater);
        }
        public void OnOutDataReceived(Object parsered, Object rawpkt, bool isCompleted)
        {
            // D-Out 포트의 수신 데이터는 무시..
        }
        public void Danger_Music()
        {
            SoundPlayer player = new SoundPlayer("../../music/BEEP.WAV");
            player.PlaySync();
        }
        public void Danger_Message(int num, int attribute)
        {
            if (MyMessage.IsOpen)
            {
                return;
            }
            if (attribute == 0)
            {

                Message.Content = "관람차의 각도 위험!! 무너져욧!!!";
                MyMessage.HorizontalOffset = 750;
                MyMessage.IsOpen = true;
            }
            else if (attribute == 1)
            {
                Message2.Content = (num - 1) + "번째 관람집의 X축 각도위험!! 무너져욧!!";
                if (MyMessage.IsOpen)
                    MyMessage2.VerticalOffset = 49 + 50;

                else
                    MyMessage2.VerticalOffset = 49;

                MyMessage2.HorizontalOffset = 670 - MyMessage.RenderSize.Width;
                MyMessage2.IsOpen = true;

            }
            else if (attribute == 2)
            {
                if (MyMessage.IsOpen)
                    MyMessage2.VerticalOffset = 49 + 50;

                else
                    MyMessage2.VerticalOffset = 49;

                Message2.Content = (num - 1) + "번째 관람집의 Y축 각도초과!! 위험합니다!!";
                MyMessage2.IsOpen = true;
                MyMessage2.VerticalOffset = 49;
                MyMessage2.HorizontalOffset = 670 - MyMessage.RenderSize.Width;
            }
        }
        /// <summary>
        /// UI 업데이트 델리게이트
        /// </summary>
        public delegate void UIUpdateDelegate();

        /// <summary>
        /// 명령어 전송 루프 메소드
        /// </summary>
        public void SendCommandLoop()
        {
            int delay = 1000;
            var sensorId = 1;
            var sensorNum = 9;

            // 포트가 연결되 있는동안 무한루프
            // 1 loop당 설정된 딜레이 적용됨. 
            while (comm != null && comm.isOpen())
            {
                try
                {
                    //루프가 실행 될때 마다 loopcount를 증가 시킴
                    UIUpdateDelegate updater = delegate
                    {
                        try
                        {
                            if (tbSensorID.Text.Length > 0 && tbSendCycle.Text.Length > 0)
                            {
                                sensorNum = int.Parse(tbSensorID.Text);
                                delay = int.Parse(tbSendCycle.Text);
                            }

                        }
                        catch (Exception e)
                        {

                        }
                    };

                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, updater);
                    comm.readSensor(sensorId);
                    Thread.Sleep(Math.Max(minimumreaddelay, delay));

                    if (comm.ResponseFlag)
                    {
                        // 응답이 있으면 설정된 딜레이 후 다음 센서를 읽어옴.
                        sensorId++;
                        if (sensorId > sensorNum)
                        {
                            sensorId = 1;
                        }
                        //           Thread.Sleep(delay);

                    }
                    else
                    {
                        //50ms 이후 응답이 없으면 쉬었다가 재전송
                        //             Thread.Sleep(200);
                    }

                }
                catch (Exception e)
                {

                }

            }

        }

        /// <summary>
        /// 이미지 위치 떔시... 절대 좌표로 맞춤..
        /// </summary>
        private static int FERRIS_POS_OFFSET_X = 135;
        private static int FERRIS_POS_OFFSET_Y = 75;
        private static int ANGLE_OFFSET = 202;
        //public void setTranslateWithAni()
        //{


        //    DoubleAnimation ELMoveY = new DoubleAnimation();

        //    ELMoveY.From = Canvas.GetTop(RotatingEl);
        //    ELMoveY.To = pointstatic.Y;
        //    ELMoveY.Duration = new Duration(TimeSpan.FromSeconds(1.0));

        //    DoubleAnimation ELMoveX = new DoubleAnimation();

        //    ELMoveX.From = Canvas.GetLeft(RotatingEl);
        //    ELMoveX.To = pointstatic.X;
        //    ELMoveX.Duration = new Duration(TimeSpan.FromSeconds(1.0));

        //    RotatingEl.BeginAnimation(Canvas.LeftProperty, ELMoveX);
        //    RotatingEl.BeginAnimation(Canvas.TopProperty, ELMoveY);
        //}

        public double[] getElmentXY(UIElement parents, UIElement child, double degree)
        {
            //Point currentPoint = new Point(vector.X, vector.Y);
            Point currentPoint = new Point(FERRIS_POS_OFFSET_X, FERRIS_POS_OFFSET_Y);
            //if (currentPoint.X == 0)
            //{
            //    currentPoint = new Point(FERRIS_POS_OFFSET_X, FERRIS_POS_OFFSET_Y);
            //}

            double r = parents.RenderSize.Width * 0.5f;
            double radians = (degree + 23) * (Math.PI / 180);
            double dx = currentPoint.X + r * Math.Cos(radians);
            double dy = currentPoint.Y + r * Math.Sin(radians);
            return new double[] { dx, dy };
        }

        public void setTranslate(UIElement parents, UIElement child, double degree)
        {

            // Return the offset vector for the TextBlock object.
            Vector vector = VisualTreeHelper.GetOffset(parents);

            //Point currentPoint = new Point(vector.X, vector.Y);
            Point currentPoint = new Point(FERRIS_POS_OFFSET_X, FERRIS_POS_OFFSET_Y);
            //if (currentPoint.X == 0)
            //{
            //    currentPoint = new Point(FERRIS_POS_OFFSET_X, FERRIS_POS_OFFSET_Y);
            //}

            double r = parents.RenderSize.Width * 0.5f;
            double radians = (degree + 23) * (Math.PI / 180);
            double dx = currentPoint.X + r * Math.Cos(radians);
            double dy = currentPoint.Y + r * Math.Sin(radians);

            TranslateTransform translate = new TranslateTransform(dx, dy);
            child.RenderTransform = translate;

        }

        public Transform getTranslate(UIElement parents, UIElement child, double degree)
        {

            // Return the offset vector for the TextBlock object.
            Vector vector = VisualTreeHelper.GetOffset(parents);

            //Point currentPoint = new Point(vector.X, vector.Y);
            Point currentPoint = new Point(FERRIS_POS_OFFSET_X, FERRIS_POS_OFFSET_Y);

            double r = parents.RenderSize.Width * 0.5f;
            double radians = (degree + 23) * (Math.PI / 180);
            double dx = currentPoint.X + r * Math.Cos(radians);
            double dy = currentPoint.Y + r * Math.Sin(radians);

            return new TranslateTransform(dx, dy);
        }


        public void setRotateWheel(UIElement child, double degree)
        {
            RotateTransform rotate = new RotateTransform(degree, child.RenderSize.Width * 0.5f, child.RenderSize.Height * 0.5f);

            child.RenderTransform = rotate;
        }

        public void setRotateFerris(int nId, double degree)
        {
            var child = wheelZone.Children[nId - 1];
            double offsetDegree = 360.0 / (double)FERRIS;
            var correctAngle = (offsetDegree * (nId - 1)) + degree + angleCorrectValue + ANGLE_OFFSET;
            var wheelAngle = (offsetDegree * (nId - 1)) + ferrisAngleList[0] + angleCorrectValue + ANGLE_OFFSET;


            TransformGroup transformGroup = new TransformGroup();
            transformGroup.Children.Add(new RotateTransform(degree, child.RenderSize.Width * 0.5f, 0));
            transformGroup.Children.Add(getTranslate(wheelImg, child, wheelAngle));

            child.RenderTransform = transformGroup;
        }


        //test retate labels
        public void initLabels()
        {

            TranslateTransform translate = new TranslateTransform(-140, -60);
            wheelZone.RenderTransform = translate;

            //8개의 label 생성
            double offsetDegree = 360.0 / (double)FERRIS;

            string[] imgUris = { "./images/blue.png", "./images/yellow.png", "./images/red.png", "./images/green.png" };
            {
                var lb = new TextBox();
                lb.Text = "Wheel";
                lb.FontSize = 20;
                lb.Width = 75;
                lb.Height = 60;
                lb.IsReadOnly = true;

                LinearGradientBrush linearGradientBrush = new LinearGradientBrush();
                linearGradientBrush.StartPoint = new Point(0, 0);
                linearGradientBrush.EndPoint = new Point(1, 1);
                linearGradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(255, 255, 255, 255), 0.0));//Yellow
                linearGradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(255, 255, 255, 255), 0.55));//Green

                lb.Background = linearGradientBrush;
                lb.Foreground = Brushes.Black;
                lb.FontWeight = FontWeights.Bold;
                lb.BorderThickness = new Thickness(0, 0, 0, 0);
                lb.VerticalContentAlignment = VerticalAlignment.Center;
                lb.HorizontalContentAlignment = HorizontalAlignment.Center;

                wheelZone.Children.Add(lb);

                translate = new TranslateTransform(FERRIS_POS_OFFSET_X + 5, FERRIS_POS_OFFSET_Y - 10);
                lb.RenderTransform = translate;

            }
            for (int i = 0; i < FERRIS; i++)
            {
                var lb = new TextBox();
                //lb.Content = "Ferris No." + (i + 1).ToString();
                lb.Text = "Ferris No." + (i + 1).ToString();
                lb.Width = 75;
                lb.Height = 75;

                // Create a diagonal linear gradient with four stops.   
                //LinearGradientBrush myLinearGradientBrush =
                //    new LinearGradientBrush();
                //myLinearGradientBrush.StartPoint = new Point(0, 0);
                //myLinearGradientBrush.EndPoint = new Point(1, 1);
                //myLinearGradientBrush.GradientStops.Add(
                //    new GradientStop(Colors.Yellow, 0.0));
                //myLinearGradientBrush.GradientStops.Add(
                //    new GradientStop(Colors.Red, 0.25));
                //myLinearGradientBrush.GradientStops.Add(
                //    new GradientStop(Colors.Blue, 0.75));
                //myLinearGradientBrush.GradientStops.Add(
                //    new GradientStop(Colors.LimeGreen, 1.0));

                lb.IsReadOnly = true;
                //lb.Background = Brushes.Coral;
                BitmapImage theImage = new BitmapImage(new Uri(imgUris[i % 4], UriKind.Relative));
                ImageBrush img = new ImageBrush(theImage);
                lb.Background = img;
                lb.Foreground = Brushes.GhostWhite;
                lb.FontWeight = FontWeights.Bold;
                lb.BorderThickness = new Thickness(0, 0, 0, 0);
                lb.VerticalContentAlignment = VerticalAlignment.Center;
                lb.HorizontalContentAlignment = HorizontalAlignment.Center;

                wheelZone.Children.Add(lb);
                //setTranslate(wheelZone, lb, (offsetDegree * i));
                wheelImg.RenderSize = new Size(600, 600);
                setTranslate(wheelImg, lb, (offsetDegree * (i + 1)));
            }

        }
        public double[] minimizeGap(double a, double b)
        {
            double[] gap = { Math.Abs(a - b), Math.Abs(360 + a - b), Math.Abs(a - (360 + b)) };
            int gapFlag;
            double from = 0, to = 0;
            if (gap[0] <= gap[1] && gap[0] <= gap[2])
            {
                gapFlag = 0;
            }
            else if (gap[1] <= gap[0] && gap[1] <= gap[2])
            {
                gapFlag = 1;
            }
            else
            {
                gapFlag = 2;
            }
            switch (gapFlag)
            {
                case 0:
                    from = a;
                    to = b;
                    break;
                case 1:
                    from = a + 360;
                    to = b;
                    break;
                case 2:
                    from = a;
                    to = b + 360;
                    break;
            }
            return new double[] { from, to };
        }
        public void updateAngleAni(double degree)
        {
            var correctAngle = degree + angleCorrectValue + ANGLE_OFFSET;

            //lastAngleList[0] = ferrisAngleList[0];
            //ferrisAngleList[0] = correctAngle;
            RotateTransform rotate = new RotateTransform(0, wheelImg.RenderSize.Width * 0.5f, wheelImg.RenderSize.Height * 0.5f);
            double[] minimized = minimizeGap(lastcorrectangle, correctAngle);

            wheelImg.RenderTransform = rotate;
            DoubleAnimation da1 = new DoubleAnimation();
            da1.From = minimized[0];
            da1.To = minimized[1];
            da1.Duration = new Duration(TimeSpan.FromMilliseconds(movingdelay));

            rotate.BeginAnimation(RotateTransform.AngleProperty, da1);
            // setRotateWheel(wheelImg, correctAngle);

            for (int i = 1; i < FERRIS + 1; i++)
            {

                TransformGroup transformGroup = new TransformGroup();
                var child = wheelZone.Children[i];
                child.RenderTransform = transformGroup;
                if (ferrisAngleList[i] != 0)
                {

                    //nId-1대신 i넣으면됨
                    //var correctAngle = (offsetDegree * (nId - 1)) + degree + angleCorrectValue + ANGLE_OFFSET;
                    // child.RenderTransform = transformGroup;
                    //setRotateFerris(i + 1, ferrisAngleList[i]);

                    double offsetDegree = 360.0 / (double)FERRIS;
                    var wheelAngle = (offsetDegree * (i)) + ferrisAngleList[0] + angleCorrectValue + ANGLE_OFFSET;

                    DoubleAnimation da = new DoubleAnimation();
                    double[] minimized2 = minimizeGap(lastAngleList[i], ferrisAngleList[i]);
                    da.From = minimized2[0];
                    da.To = minimized2[1];
                    da.Duration = new Duration(TimeSpan.FromMilliseconds(movingdelay));
                    //     da.RepeatBehavior = RepeatBehavior.Forever;
                    RotateTransform rt = new RotateTransform(0, child.RenderSize.Width * 0.5f, 0);
                    transformGroup.Children.Add(rt);
                    //                   transformGroup.Children.Add(getTranslate(wheelImg, child, wheelAngle));
                    rt.BeginAnimation(RotateTransform.AngleProperty, da);

                }
                updateLabelTranslate(i, correctAngle, transformGroup);


            }
            lastcorrectangle = correctAngle;
        }
        public void updateLabelTranslate(int idx, double degree, TransformGroup transformGroup)
        {
            double offsetDegree = 360.0 / (double)FERRIS;

            var lb = wheelZone.Children[idx];
            if (idx != 0)
            {
                double lastangle = (offsetDegree) * idx + lastcorrectangle;
                double nowangle = (offsetDegree) * idx + degree;
                Point currentPoint = new Point(FERRIS_POS_OFFSET_X, FERRIS_POS_OFFSET_Y);

                double r = wheelImg.RenderSize.Width * 0.5f;
                double radians = (degree + 23) * (Math.PI / 180);
                double dx = currentPoint.X + r * Math.Cos(radians);
                double dy = currentPoint.Y + r * Math.Sin(radians);

                double[] XY1 = getElmentXY(wheelImg, lb, lastangle);
                double[] XY2 = getElmentXY(wheelImg, lb, nowangle);

                TranslateTransform translate = new TranslateTransform();


                DoubleAnimation ELMoveY = new DoubleAnimation();

                ELMoveY.From = XY1[1];
                ELMoveY.To = XY2[1];
                ELMoveY.Duration = new Duration(TimeSpan.FromMilliseconds(movingdelay));

                DoubleAnimation ELMoveX = new DoubleAnimation();

                ELMoveX.From = XY1[0];
                ELMoveX.To = XY2[0];
                ELMoveX.Duration = new Duration(TimeSpan.FromMilliseconds(movingdelay));
                transformGroup.Children.Add(translate);

                translate.BeginAnimation(TranslateTransform.XProperty, ELMoveX);
                translate.BeginAnimation(TranslateTransform.YProperty, ELMoveY);

                //setTranslate(wheelImg, lb, (offsetDegree * idx) + degree);

            }

        }


        public void updateLabelContens(int idx, string value)
        {
            var lb = wheelZone.Children[idx - 1] as TextBox;

            if (idx == 1)
            {
                lb.Text = "Wheel" + Environment.NewLine + value;
            }
            else
            {
                lb.Text = "Ferris No." + (idx - 1).ToString() + Environment.NewLine + value;
            }

            //lb.Content = "Ferris No." + (idx + 1).ToString() + Environment.NewLine + value;
        }

        private void btnAngleUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (tbAngleCorrection.Text.Length > 0)
                {
                    angleCorrectValue = int.Parse(tbAngleCorrection.Text);
                }

                updateAngleAni(ferrisAngleList[0]);
            }
            catch (Exception ex)
            {

            }

        }



        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) //타이틀 바에 마우스 왼쪽 버튼을 누르면
        {
            this.DragMove();
        }

        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Btn_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Hand;
        }

        private void Btn_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
        }

        private void ExitBtn_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            Button b = sender as Button;
            b.ToolTip = "닫기";
        }

        private void SettingBtn_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            Button b = sender as Button;
            b.ToolTip = "형상 관리 설정";
        }

        private void SettingBtn_Click(object sender, RoutedEventArgs e)
        {
            MyPopup.IsOpen = true;
        }
        private void PopClose2_Click(object sender, RoutedEventArgs e)
        {
            MyMessage.IsOpen = false;
        }
        private void PopClose3_Click(object sender, RoutedEventArgs e)
        {
            MyMessage2.IsOpen = false;
        }
        private void PopClose_Click(object sender, RoutedEventArgs e)
        {
            MyPopup.IsOpen = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            logDatas.Clear();
            logView.ItemsSource = logDatas;
            logView.ColumnWidth = 50;
            logView.FontSize = 20;
            this.logView.Columns[1].Width = 200;
        }
    }
}
