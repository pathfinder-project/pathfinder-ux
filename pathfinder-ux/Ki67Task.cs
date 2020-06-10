using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA2000
#pragma warning disable CA2234

namespace PathFinder
{
    class Ki67Task
    {
        private Thread th;
        private double _result;
        private List<double> x;
        private List<double> y;
        private byte[] region; // bmp image stream
        private int head;

        public Ki67Task(int head, byte[] region, List<double> x, List<double> y)
        {
            _result = double.NaN;
            this.region = region;
            this.x = x;
            this.y = y;
            this.head = head;
            th = new Thread(new ThreadStart(Work));
        }

        public void Start()
        {
            th.Start();
        }

        public void Work()
        {
            var s = ComputeKi67PositiveRate();
            //Console.WriteLine(s);
            Result = double.Parse(s);
        }

        public string ComputeKi67PositiveRate()
        {
            using (var client = new HttpClient())
            {
                MemoryStream ms = new MemoryStream(region);
                var form = new MultipartFormDataContent
                {
                    {new StringContent(string.Join(" ", x.ToArray())), "x_coordinates" },
                    {new StringContent(string.Join(" ", y.ToArray())), "y_coordinates" },
                    //{new ByteArrayContent(region), "img" }
                    {new StreamContent(ms), "img", $"{head}.bmp" },
                };

                var response = client.PostAsync(@"http://127.0.0.1:1919/ki67", form).Result;
                string content = response.Content.ReadAsStringAsync().Result;
                return content;
            }
        }

        public double Result
        {
            get
            {
                lock (this)
                {
                    return _result;
                }
            }

            set
            {
                lock (this)
                {
                    _result = value;
                }
            }
        }

        public int HeadId { get { return head; } }
    }
}
