using System;
using System.Collections.Generic;
using System.Linq;
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
using System.IO.Ports;
using System.Windows.Threading;
using System.Threading;
using System.Media;
using Npgsql;

namespace ArduinoCommunicationTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int INSIDE_PLAYBACK_THRESHOLD = 10;
        const int OUTSIDE_PLAYBACK_THRESHOLD = 10;
        const int PRULLENBAK_DEPTH = 30;
        const int NOTIFICATION_THRESHOLD = 75;

        double ScreenHeight = SystemParameters.PrimaryScreenHeight;
        double ScreenWidth = SystemParameters.PrimaryScreenWidth;

        int PapierCount = 0;
        int PlasticCount = 0;

        SerialPort port = new SerialPort("COM8", 9600);

        DispatcherTimer mainTimer = new DispatcherTimer();
        DispatcherTimer timeOutTimer = new DispatcherTimer();
        DispatcherTimer BakVolTimeOutTimer = new DispatcherTimer();

        Label lblCounterPapier;
        Label lblCounterPlastic;
        Label lblVol;
        StackPanel stkTimestamps;

        bool canPlaySound = true;
        bool isTriggered = false;

        SoundPlayer PapierHier;
        SoundPlayer Dankjewel;

        Random r = new Random();

        public MainWindow()
        {
            SetupInterface();
            DrawTimeStamps();
            mainTimer.Interval = TimeSpan.FromMilliseconds(100);
            mainTimer.Tick += Timer_Tick;
            mainTimer.Start();

            timeOutTimer.Interval = TimeSpan.FromSeconds(1d);
            timeOutTimer.Tick += TimeOutTimer_Tick;

            BakVolTimeOutTimer.Interval = TimeSpan.FromSeconds(5d);
            BakVolTimeOutTimer.Tick += BakVolTimeOutTimer_Tick;

            PapierHier = new SoundPlayer("HolleBolleDaanPapierHier.wav");
            Dankjewel = new SoundPlayer("HolleBolleDaanDankjewel.wav");
            InitializeComponent();


            if (!port.IsOpen)
                port.Open();

            lblVol.Visibility = Visibility.Collapsed;
        }



        private void Timer_Tick(object sender, EventArgs e)
        {
            string[] message = GetMessage().Replace("\r\n", string.Empty).Split("="[0]);

            List<int> list1 = new List<int>();
            List<int> list2 = new List<int>();
            double gemiddeldeAfstandInside = 100;
            double gemiddeldeAfstandOutside = 100;

            foreach (string m in message)
            {
                try
                {
                    if (m.StartsWith("1:"))
                    {
                        list1.Add(Convert.ToInt32(m.Substring(2)));
                    }
                    else if (m.StartsWith("2:"))
                    {
                        list2.Add(Convert.ToInt32(m.Substring(2)));
                    }
                }
                catch
                {

                }
            }

            if (list1.Count() != 0)
                gemiddeldeAfstandInside = list1.Average();

            if (list2.Count() != 0)
                gemiddeldeAfstandOutside = list2.Average();

            

            if (((gemiddeldeAfstandInside <= INSIDE_PLAYBACK_THRESHOLD) && canPlaySound) || isTriggered)
            {
                if (isTriggered == false)
                {
                    stkTimestamps.Children.Clear();
                    Dankjewel.PlaySync();
                    timeOutTimer.Start();
                    canPlaySound = false;
                    InsertTimeStampToDatabase();
                    DrawTimeStamps();

                    if (r.Next(1, 3) == 1)
                        PapierCount++;
                    else
                        PlasticCount++;
                    isTriggered = true;
                }
                else
                {
                    if (gemiddeldeAfstandInside > INSIDE_PLAYBACK_THRESHOLD)
                    {
                        isTriggered = false;
                        lblVol.Visibility = Visibility.Collapsed;
                    }
                    BakVolTimeOutTimer.Start();
                }
            }




            if ((gemiddeldeAfstandOutside <= OUTSIDE_PLAYBACK_THRESHOLD) && canPlaySound)
            {
                PapierHier.PlaySync();
                timeOutTimer.Start();
                canPlaySound = false;
            }

            lblCounterPapier.Content = PapierCount.ToString();
            lblCounterPlastic.Content = PlasticCount.ToString();
        }



        private string GetMessage()
        {
            string message = port.ReadExisting();
            return message;
        }

        private void TimeOutTimer_Tick(object sender, EventArgs e)
        {
            timeOutTimer.Stop();
            canPlaySound = true;
        }

        private void SetupInterface()
        {
            this.WindowState = WindowState.Maximized;
            this.ResizeMode = ResizeMode.NoResize;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.WindowStyle = WindowStyle.None;
            this.KeyDown += MainWindow_KeyDown;

            Grid grdMain = new Grid();

            lblCounterPapier = new Label()
            {
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                Content = PapierCount,
                FontSize = 120,
                FontFamily = new FontFamily("Archive"),
                Margin = new Thickness(ScreenWidth / 8 * 2, ScreenHeight / 2, 0, 0),
                Foreground = Brushes.White,
                Width = 150,
                Height = 150
            };

            lblCounterPlastic = new Label()
            {
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                Content = PlasticCount,
                FontSize = 120,
                FontFamily = new FontFamily("Archive"),
                Margin = new Thickness(ScreenWidth / 8 * 5.5d, ScreenHeight / 2, 0, 0),
                Foreground = Brushes.White,
                Width = 150,
                Height = 150
            };

            Image imgBackGround = new Image()
            {
                Source = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "ScoreBoard.jpg")),
                Width = ScreenWidth,
                Height = ScreenHeight
            };

            lblVol = new Label()
            {
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Content = "BAK VOL!",
                FontSize = 80,
                FontFamily = new FontFamily("Archive"),
                Margin = new Thickness(0, 200, 0, 0),
                Foreground = Brushes.White,
                Width = ScreenWidth,
                Height = 150
            };

            stkTimestamps = new StackPanel()
            {
                Name = "stkTimestamps",
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = 100,
                Margin = new Thickness(0, 585, 0, 0),
            };

            this.AddChild(grdMain);
            grdMain.Children.Add(imgBackGround);
            grdMain.Children.Add(lblCounterPapier);
            grdMain.Children.Add(lblCounterPlastic);
            grdMain.Children.Add(lblVol);
            grdMain.Children.Add(stkTimestamps);
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private static void InsertTimeStampToDatabase()
        {
            try
            {
                using (var conn = new NpgsqlConnection("Host=localhost;Username=postgres;Password=student;Database=postgres"))
                {
                    string query = "INSERT INTO wastemeister (id) VALUES (1);";
                    conn.Open();
                    using (var command = new NpgsqlCommand(query, conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Console.WriteLine("{0} {1}", reader.GetString(0), reader.GetString(1));
                            }
                        }
                    }
                }
            }
            catch (NpgsqlException e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private List<DateTime> GetTable()
        {
            List<DateTime> temp = new List<DateTime>();
            using (var conn = new NpgsqlConnection("Host=localhost;Username=postgres;Password=student;Database=postgres"))
            {
                string query = "SELECT * FROM wastemeister ORDER BY timestamp desc LIMIT 5;";
                conn.Open();
                using (var command = new NpgsqlCommand(query, conn))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            temp.Add(reader.GetDateTime(1));
                        }
                    }
                }
            }
            return temp;
        }
        private void BakVolTimeOutTimer_Tick(object sender, EventArgs e)
        {
            if (isTriggered)
            {
                lblVol.Visibility = Visibility.Visible;
                BakVolTimeOutTimer.Stop();
            }
        }

        private void DrawTimeStamps()
        {
            foreach (var i in GetTable())
            {
                Label l = new Label()
                {
                    Content = i.ToString(),
                    Width = 150,
                    Height = 25,
                    FontSize = 12,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                stkTimestamps.Children.Add(l);
            }
        }
    }
}
