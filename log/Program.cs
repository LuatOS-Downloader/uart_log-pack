using System.IO.Ports;

namespace log
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var  pack = args[0];
            var cmd = args[1];

            switch(cmd)
            {
                case "list":
                    ShowDevices();
                    break;
                case "reset":
                    ResetDevice(args[2]);
                    break;
                case "log":
                    LogDevice(args[2], int.Parse(args[3]));
                    break;
            }
        }

        private static void ShowDevices()
        {
            var list = SerialPort.GetPortNames();
            foreach (var name in list)
                Console.WriteLine(name);
        }

        private static void ResetDevice(string port)
        {
            var p = new SerialPort(port);
            p.Open();
            p.RtsEnable = true;
            Thread.Sleep(500);
            p.RtsEnable = false;
            p.Close();
        }

        //收到串口事件的信号量
        private static EventWaitHandle WaitUartReceive = new AutoResetEvent(true);
        private static void LogDevice(string port, int br)
        {
            var output = Console.OpenStandardOutput();
            var p = new SerialPort(port);
            p.BaudRate = br;
            p.DataReceived += (_,_) =>
            {
                WaitUartReceive.Set();
            };
            p.Open();
            p.RtsEnable = false;

            WaitUartReceive.Reset();
            while (true)
            {
                WaitUartReceive.WaitOne();
                List<byte> result = new List<byte>();
                while (true)//循环读
                {
                    if (!p.IsOpen)//串口被关了，不读了
                    {
                        Console.WriteLine("\r\ndisconnected!");
                        return;
                    }
                    try
                    {
                        int length = p.BytesToRead;
                        if (length == 0)//没数据，退出去
                            break;
                        byte[] rev = new byte[length];
                        p.Read(rev, 0, length);//读数据
                        if (rev.Length == 0)
                            break;
                        output.Write(rev);
                        output.Flush();
                    }
                    catch { return; }//崩了？
                }
            }

            p.Close();
        }
    }
}