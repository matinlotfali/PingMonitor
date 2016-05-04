using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
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

namespace PingMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string dataFile = "data.txt";
        readonly BitmapImage Sample07 = new BitmapImage(new Uri("pack://application:,,,/Resources/Sample 07.png"));
        readonly BitmapImage Sample02 = new BitmapImage(new Uri("pack://application:,,,/Resources/Sample 02.png"));
        readonly BitmapImage info = new BitmapImage(new Uri("pack://application:,,,/Resources/Button Blue Info.png"));
        readonly System.Windows.Forms.NotifyIcon notifyIcon = new System.Windows.Forms.NotifyIcon();

        public MainWindow()
        {
            InitializeComponent();

            notifyIcon.Icon = Properties.Resources.info;
            notifyIcon.Visible = true;
            notifyIcon.Click += NotifyIcon_Click;

            if (File.Exists(dataFile))
            {
                var data = File.ReadAllLines(dataFile);
                for (int i = 0; i < data.Length; i += 2)
                    dataGrid.Items.Add(new Item(data[i], data[i + 1]));
                ErrorTimer_Tick(null, null);
            }

            DispatcherTimer errorTimer = new DispatcherTimer();
            errorTimer.Interval = new TimeSpan(0, 0, 1);
            errorTimer.Tick += ErrorTimer_Tick;
            errorTimer.Start();

            DispatcherTimer normalTimer = new DispatcherTimer();
            normalTimer.Interval = new TimeSpan(0, 0, 30);
            normalTimer.Tick += NormalTimer_Tick;
            normalTimer.Start();
        }

        private void NotifyIcon_Click(object sender, EventArgs e)
        {
            if (Visibility == Visibility.Visible)
                WindowState = WindowState.Minimized;
            else
            {
                Show();
                WindowState = WindowState.Normal;
            }
        }

        private void NormalTimer_Tick(object sender, EventArgs e)
        {
            foreach (var i in dataGrid.Items)
            {
                var item = (Item)i;
                if (item.Time != null)
                    PingItem(item);
            }            
        }

        private void ErrorTimer_Tick(object sender, EventArgs e)
        {
            foreach (var i in dataGrid.Items)
            {
                var item = (Item)i;
                if (item.Time == null)
                    PingItem(item);
            }
        }

        private async void PingItem(Item item)
        {
            using (Ping ping = new Ping())
            {
                PingReply reply = null;
                try
                {
                    reply = await ping.SendPingAsync(item.URL);
                }
                catch { }

                if (reply != null && reply.Status == IPStatus.Success)
                {
                    long time = reply.RoundtripTime;
                    if (item.Time != time || item.Image != Sample07)
                    {
                        item.Time = time;
                        item.Image = Sample07;
                    }
                }
                else
                {
                    if (item.Time != null || item.Image != Sample02)
                    {
                        item.Time = null;
                        item.Image = Sample02;
                    }
                }
            }
            RefreshStatus();
            GC.Collect();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
                Hide();
        }

        private void RefreshStatus()
        {
            bool foundError = false;
            bool foundSuccess = false;
            foreach (var i in dataGrid.Items)
                if (((Item)i).Time == null)
                    foundError = true;
                else
                    foundSuccess = true;

            if (dataGrid.Columns[2].SortDirection == ListSortDirection.Ascending)
            {
                for (int i = 0; i < dataGrid.Items.Count; i++)
                    for (int j = i + 1; j < dataGrid.Items.Count; j++)
                        if (Compare(i, j))
                        {
                            var temp = dataGrid.Items[i];
                            dataGrid.Items[i] = dataGrid.Items[j];
                            dataGrid.Items[j] = temp;
                        }
            }
            if (dataGrid.Columns[2].SortDirection == ListSortDirection.Descending)
            {
                for (int i = 0; i < dataGrid.Items.Count; i++)
                    for (int j = i + 1; j < dataGrid.Items.Count; j++)
                        if (!Compare(i, j))
                        {
                            var temp = dataGrid.Items[i];
                            dataGrid.Items[i] = dataGrid.Items[j];
                            dataGrid.Items[j] = temp;
                        }
            }
            dataGrid.Items.Refresh();

            if (foundSuccess && !foundError)
            {
                StatusText.Text = "All Successful";
                StatusImage.Source = Sample07;
                notifyIcon.Icon = Properties.Resources.Sample_071;
            }
            else if (foundSuccess && foundError)
            {
                StatusText.Text = "Partial Successful";
                StatusImage.Source = info;
                notifyIcon.Icon = Properties.Resources.info;
            }
            else
            {
                StatusText.Text = "All Timedout";
                StatusImage.Source = Sample02;
                notifyIcon.Icon = Properties.Resources.Sample_021;
            }

            notifyIcon.Text = StatusText.Text;
        }

        private bool Compare(int i, int j)
        {
            Item a = (Item)dataGrid.Items[i];
            Item b = (Item)dataGrid.Items[j];
            if (!a.Time.HasValue && b.Time.HasValue)
                return false;
            else if (a.Time.HasValue && !b.Time.HasValue)
                return true;
            else if (a.Time.HasValue && b.Time.HasValue)
                return a.Time > b.Time;
            return false;
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            dataGrid.Items.Remove(dataGrid.SelectedItem);
            Save();
        }

        private void Save()
        {
            List<string> str = new List<string>();
            foreach (var i in dataGrid.Items)
            {
                var item = (Item)i;
                str.Add(item.Name);
                str.Add(item.URL);
            }
            File.WriteAllLines(dataFile, str);
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddWindow window = new AddWindow();
            if (window.ShowDialog() == true)
            {
                dataGrid.Items.Add(new Item(window.name, window.url));
                Save();
                RefreshStatus();
            }
        }
    }
}
