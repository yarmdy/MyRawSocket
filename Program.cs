// See https://aka.ms/new-console-template for more information
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

Console.WriteLine("Hello, World!");

var socket = new Socket(AddressFamily.InterNetwork,SocketType.Raw,ProtocolType.IP);
//socket.Blocking = false;
socket.Bind(new IPEndPoint(IPAddress.Parse("192.168.0.11"),0));

socket.SetSocketOption(SocketOptionLevel.IP,SocketOptionName.HeaderIncluded,true);

byte[] optionIn = new byte[] { 1,0,0,0};
byte[] optionOut = new byte[4];

//const int SIO_RCVALL = unchecked((int)0x98000001);
socket.IOControl(IOControlCode.ReceiveAll, optionIn, optionOut);


byte[] buffer = new byte[102400];
while (true)
{
    try
    {
        int bytesRead = await socket.ReceiveAsync(buffer);
        
        IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<IPHeader>());
        Marshal.Copy(buffer, 0, ptr, Marshal.SizeOf<IPHeader>());
        IPHeader header = Marshal.PtrToStructure<IPHeader>(ptr);
        var sIP = IPAddress.Parse(string.Join(".", BitConverter.GetBytes(header.SourceAddress)));
        var tIP = IPAddress.Parse(string.Join(".", BitConverter.GetBytes(header.DestinationAddress)));
        var protocol = header.Protocol;
        if (bytesRead > 0)
        {
            // 解析 ICMP 数据包
            Console.WriteLine($"接收：({protocol}){sIP}->{tIP}【{bytesRead}】");
        }
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
    public byte DataOffsetAndFlags;     // 数据偏移和标志位，1字节
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
    public ushort FlagsAndOffset;         // 标志位和片偏移
    public byte TimeToLive;               // 存活时间
    public byte Protocol;                 // 协议
    public ushort HeaderChecksum;         // 头部校验和
    public uint SourceAddress;            // 源IP地址
    public uint DestinationAddress;       // 目标IP地址
}