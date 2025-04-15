using System.Diagnostics;

class SerialBridge
{
    private Process? _process;
    private readonly string _binaryPath;
    private readonly string _port;
    private readonly int _baudRate;

    public SerialBridge(string binaryPath, string port, int baudRate)
    {
        _binaryPath = binaryPath;
        _port = port;
        _baudRate = baudRate;
    }

    public void Start()
    {
        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _binaryPath,
                Arguments = $"-d {_port} -b {_baudRate}",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        _process.Exited += (sender, args) =>
        {
            Console.WriteLine("Serial service exited. Restarting...");
            Start();
        };

        _process.Start();

        Task.Run(() => ReadFromSerial());
    }

    private void ReadFromSerial()
    {
        if (_process == null || _process.StandardOutput == null)
            return;

        var reader = _process.StandardOutput;
        while (!_process.HasExited)
        {
            var line = reader.ReadLine();
            if (line != null)
            {
                Console.WriteLine($"[SERIAL] {line}");
            }
        }
    }

    public void Send(string data)
    {
        if (_process?.StandardInput != null && !_process.HasExited)
        {
            _process.StandardInput.WriteLine(data);
            _process.StandardInput.Flush();
        }
    }

    public void Stop()
    {
        if (_process != null && !_process.HasExited)
        {
            _process.Kill();
            _process.WaitForExit();
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        string binaryPath = "../serial_service/build/serial_service";
        string serialPort = "/dev/tty.usbserial-123";
        int baudRate = 9600;

        var bridge = new SerialBridge(binaryPath, serialPort, baudRate);
        bridge.Start();

        Console.WriteLine("Type messages to send to the serial device. Press Ctrl+C to exit.");

        Console.CancelKeyPress += (sender, e) =>
        {
            Console.WriteLine("Exiting...");
            bridge.Stop();
            Environment.Exit(0);
        };

        while (true)
        {
            string? line = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(line))
            {
                bridge.Send(line);
            }
        }
    }
}
