using System.Windows.Media.Imaging;

namespace PingMonitor
{
    class Item
    {
        public BitmapSource Image { get; set; }
        public string Name { get; private set; }
        public string URL { get; private set; }
        public long? Time { get; set; }

        public Item(string name, string url)
        {
            Name = name;
            URL = url;
        }
    }
}
