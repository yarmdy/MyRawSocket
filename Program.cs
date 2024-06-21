// See https://aka.ms/new-console-template for more information
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

void toLittle<T>(byte[] data,int offset) where T:struct
{ 
    var tSize = Marshal.SizeOf(typeof(T));
    var loc = offset;
    if (data.Length < offset + tSize)
    {
        return;
    }
    foreach(var field in typeof(T).GetFields())
    {
        var fSize = Marshal.SizeOf(field.FieldType);
        var i = loc;
        loc += fSize;
        if (fSize < 2 || fSize%2>0)
        {
            continue;
        }
        

        var lf = 0;
        do
        {
            var tmp = data[i + lf];
            data[i + lf] = data[i+fSize-1-lf];
            data[i + fSize - 1 - lf] = tmp;
        } while (++lf < fSize / 2);
    }
}

var socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);
//socket.Blocking = false;
var ips = Dns.GetHostAddresses(Dns.GetHostName());
Console.WriteLine("请选择以下IP地址，并输入其中的序号：");
var index = 0;
ips.ToList().ForEach(ip =>Console.WriteLine($"{++index}: {ip}"));
while (true)
{
    var key = Console.ReadKey();
    if(!int.TryParse(key.KeyChar.ToString(),out index))
    {
        continue;
    }
    if (index > ips.Length || index < 1) {  
        continue; 
    }
    break;
}
var myip = ips[index - 1];
socket.Bind(new IPEndPoint(myip, 0));

socket.SetSocketOption(SocketOptionLevel.IP,SocketOptionName.HeaderIncluded,true);

byte[] optionIn = new byte[] { 1,0,0,0};
byte[] optionOut = new byte[4];

//const int SIO_RCVALL = unchecked((int)0x98000001);
socket.IOControl(IOControlCode.ReceiveAll, optionIn, optionOut);


byte[] buffer = new byte[102400];
var sizeTcpHeader = Marshal.SizeOf<TcpHeader>();
var sizeIPHeader = Marshal.SizeOf<IPHeader>();
while (true)
{
    try
    {
        int bytesRead = socket.Receive(buffer);
        var data = buffer.Take(bytesRead).ToArray();
#pragma warning disable CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
        Task.Run(() => {
            toLittle<IPHeader>(data, 0);
            toLittle<TcpHeader>(data, sizeIPHeader);

            var ptr = Marshal.AllocHGlobal(sizeIPHeader);
            Marshal.Copy(data, 0, ptr, sizeIPHeader);
            IPHeader header = Marshal.PtrToStructure<IPHeader>(ptr);
            Marshal.FreeHGlobal(ptr);
            var sIP = IPAddress.Parse(string.Join(".", BitConverter.GetBytes(header.SourceAddress).Reverse()));
            var tIP = IPAddress.Parse(string.Join(".", BitConverter.GetBytes(header.DestinationAddress).Reverse()));

            var protocol = header.Protocol;
            

            if (protocol != 6)
            {
                return;
            }
            ptr = Marshal.AllocHGlobal(sizeTcpHeader);
            Marshal.Copy(data, sizeIPHeader, ptr, sizeTcpHeader);
            var tcpheader = Marshal.PtrToStructure<TcpHeader>(ptr);
            Marshal.FreeHGlobal(ptr);
            if (!tIP.Equals(myip))
            {
                return;
            }
            Console.WriteLine($"TCP：{sIP}:{tcpheader.SourcePort}->{tIP}:{tcpheader.DestinationPort}【{bytesRead}】");

        }).Wait();

    }
    catch(Exception ex)
    {
        Console.WriteLine(ex.ToString());
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct TcpHeader
{
    public ushort SourcePort;           // 源端口号，2字节
    public ushort DestinationPort;      // 目标端口号，2字节
    public uint SequenceNumber;         // 序列号，4字节
    public uint AcknowledgmentNumber;   // 确认号，4字节
    public ushort DataOffsetAndFlags;     // 数据偏移和标志位，1字节
    public ushort WindowSize;           // 窗口大小，2字节
    public ushort Checksum;             // 校验和，2字节
    public ushort UrgentPointer;        // 紧急指针，2字节
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct IPHeader
{
    public byte VersionAndHeaderLength;   // 版本和头部长度
    public byte DifferentiatedServices;   // 区分服务
    public ushort TotalLength;            // 总长度
    public ushort Identification;         // 标识
    public ushort FlagsAndFragmentOffset; // 标志位和片偏移
    public byte TimeToLive;               // 存活时间
    public byte Protocol;                 // 协议
    public ushort HeaderChecksum;         // 头部校验和
    public uint SourceAddress;            // 源IP地址
    public uint DestinationAddress;       // 目标IP地址
}

