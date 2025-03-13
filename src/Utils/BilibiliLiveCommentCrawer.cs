using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;

namespace TnTRFMod.Utils;

// Referenced from https://github.com/a820715049/BiliBiliLive

public class BilibiliLiveCommentCrawer(long RoomId)
{
    private const string CIDInfoUrl = "https://api.live.bilibili.com/room/v1/Danmu/getConf?room_id=";
    private readonly string[] defaultPaths = ["livecmt-2.bilibili.com", "livecmt-1.bilibili.com"];
    private readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(5) };
    private readonly short protocolversion = 2;
    private string chatPath = "chat.bilibili.com";
    private int chatPort = 2243;
    private TcpClient client;
    private bool connected = false;
    private CancellationTokenSource cts = new();
    private Stream netStream;

    private Task startTask;

    public async Task Start()
    {
        await Stop();
        cts = new CancellationTokenSource();
        startTask = StartAsync();
    }

    public async Task Stop()
    {
        if (startTask == null) return;
        cts.Cancel();
        await startTask;
    }

    private async Task HeartbeatLoop()
    {
        try
        {
            while (cts.Token.IsCancellationRequested == false)
            {
                //每30秒发送一次 心跳
                await SendHeartbeatAsync();
                await Task.Delay(30000, cts.Token);
            }
        }
        catch (Exception e)
        {
            await Stop();
        }
    }

    private async Task SendHeartbeatAsync()
    {
        await SendSocketDataAsync(2);
    }

    public async Task StartAsync()
    {
        await Console.Out.WriteLineAsync($"开始连接直播间 {RoomId}");
        await Console.Out.WriteLineAsync("正在获取弹幕服务器地址");

        var token = "";

        try
        {
            var url = CIDInfoUrl + RoomId;
            var resText = await httpClient.GetStringAsync(url, cts.Token);
            await Console.Out.WriteLineAsync(resText);
            var res = JsonNode.Parse(resText);
            token = res["data"]["token"].ToString();
            chatPath = res["data"]["host"].ToString();
            chatPort = res["data"]["port"].GetValue<int>();
            if (string.IsNullOrEmpty(chatPath)) throw new Exception();
        }
        catch (Exception e)
        {
            chatPath = defaultPaths[Random.Shared.Next(0, defaultPaths.Length)];
            await Console.Out.WriteLineAsync($"获取弹幕服务器地址时出现错误，尝试使用默认服务器... 错误信息: {e}");
        }

        client = new TcpClient();

        await Console.Out.WriteLineAsync($"正在连接到 {chatPath}:{chatPort}");
        await client.ConnectAsync(chatPath, chatPort);
        netStream = Stream.Synchronized(client.GetStream());

        await Console.Out.WriteLineAsync("正在加入房间");

        await SendJoinChannel(token);
        
        await Console.Out.WriteLineAsync("加入成功！正在接收弹幕信息");

        var heartbeatLoop = HeartbeatLoop();
        var receiveMessageLoop = ReceiveMessageLoop();
        
        await Task.WhenAll(heartbeatLoop, receiveMessageLoop);
    }

    private async Task ReceiveMessageLoop()
    {
        var headerBuffer = new byte[16];
        var bodyBuffer = new byte[1];

        while (cts.Token.IsCancellationRequested == false && client.Connected)
        {
            try
            {
                await ReadAllAsync(headerBuffer);
                var header = DanmakuProtocol.FromBuffer(headerBuffer);
                if (header.PacketLength < 16)
                {
                    await Console.Out.WriteLineAsync("数据包长度小于16");
                    continue;
                }

                var payloadLength = header.PacketLength - headerBuffer.Length;
                if (payloadLength == 0) continue;
                Array.Resize(ref bodyBuffer, payloadLength);
                await ReadAllAsync(bodyBuffer);

                await Console.Out.WriteLineAsync("接收到报文: " + Encoding.UTF8.GetString(bodyBuffer));
            }
            catch (Exception e)
            {
                if (e is ObjectDisposedException)
                {
                    await Console.Out.WriteLineAsync("连接已释放");
                    break;
                } else if (e is IOException)
                {
                    await Console.Out.WriteLineAsync("连接发生错误 " + e);
                    break;
                }
                else
                {
                    await Console.Out.WriteLineAsync("接收消息时出现错误: " + e);
                }
            }
        }
        cts.Cancel();
    }
    
    private async Task ReadAllAsync(byte[] buffer)
    {
        var offset = 0;
        var length = buffer.Length;
        while (length > 0)
        {
            var read = await netStream.ReadAsync(buffer.AsMemory(offset, length), cts.Token);
            if (read == 0) throw new EndOfStreamException();
            offset += read;
            length -= read;
        }
    }

    private Task SendSocketDataAsync(int action, string body = "")
    {
        return SendSocketDataAsync(0, 16, protocolversion, action, 1, body);
    }

    private async Task SendSocketDataAsync(int packetLength, short magic, short ver, int action, int param = 1,
        string body = "")
    {
        var payload = Encoding.UTF8.GetBytes(body);
        if (packetLength == 0) packetLength = payload.Length + 16;
        var buffer = new byte[packetLength];

        BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(0, 4), buffer.Length);
        BinaryPrimitives.WriteInt16BigEndian(buffer.AsSpan(4, 2), magic);
        BinaryPrimitives.WriteInt16BigEndian(buffer.AsSpan(6, 2), ver);
        BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(8, 4), action);
        BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(12, 4), param);

        if (payload.Length > 0)
            payload.CopyTo(buffer.AsSpan(16));

        await Console.Out.WriteLineAsync(Encoding.UTF8.GetString(buffer));
        
        await netStream.WriteAsync(buffer, cts.Token);
    }

    private async Task SendJoinChannel(string token)
    {
        // var packetModel = new JsonNode{ roomid = channelId, uid = 0, protover = 2, token = token, platform = "danmuji" };
        var payload = new JsonObject
        {
            ["roomid"] = RoomId,
            ["uid"] = 0,
            ["protover"] = 2,
            ["token"] = token,
            ["platform"] = "danmuji"
        }.ToJsonString();
        await Console.Out.WriteLineAsync(payload);
        await SendSocketDataAsync(7, payload);
    }
    

    public static void Test()
    {
        var crawer = new BilibiliLiveCommentCrawer(6464950);
        crawer.StartAsync().Wait();
        Console.Out.WriteLine("运行结束");
    }
    
    internal struct DanmakuProtocol
    {
        /// <summary>
        /// 消息总长度 (协议头 + 数据长度)
        /// </summary>
        public int PacketLength;
        /// <summary>
        /// 消息头长度 (固定为16[sizeof(DanmakuProtocol)])
        /// </summary>
        public short HeaderLength;
        /// <summary>
        /// 消息版本号
        /// </summary>
        public short Version;
        /// <summary>
        /// 消息类型
        /// </summary>
        public int Action;
        /// <summary>
        /// 参数, 固定为1
        /// </summary>
        public int Parameter;

        internal static DanmakuProtocol FromBuffer(byte[] buffer)
        {
            if (buffer.Length < 16) { throw new ArgumentException(); }
            return new DanmakuProtocol()
            {
                PacketLength = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(0, 4)),
                HeaderLength = BinaryPrimitives.ReadInt16BigEndian(buffer.AsSpan(4, 2)),
                Version = BinaryPrimitives.ReadInt16BigEndian(buffer.AsSpan(6, 2)),
                Action = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(8, 4)),
                Parameter = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(12, 4))
            };
        }
    }
}