using System.Buffers.Binary;
using System.IO.Compression;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;

namespace TnTRFMod.Utils;

public class BilibiliLiveCommentCrawer
{
    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36";

    private const string CIDInfoUrl =
        "https://api.live.bilibili.com/xlive/web-room/v1/index/getDanmuInfo";

    private readonly string _sessionData;

    private readonly HttpClient httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5), DefaultRequestHeaders =
        {
            { "User-Agent", UserAgent },
            { "Accept", "application/json, text/plain, */*" },
            { "Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8" },
            { "Referer", "https://live.bilibili.com/" },
            { "Origin", "https://live.bilibili.com" }
        }
    };

    private readonly short protocolversion = 2;
    private readonly long roomId;
    private string chatHost = "chat.bilibili.com";
    private int chatPort = 2243;
    private TcpClient client;
    private CancellationTokenSource cts = new();
    private Stream netStream;

    private Task startTask;
    private string token = "";
    private long userId;

    public BilibiliLiveCommentCrawer(long roomId, string sessionData = "")
    {
        _sessionData = sessionData;
        this.roomId = roomId;
    }

    public event EventHandler<DammakuMessage> OnDanmakuMessage;

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
            while (!cts.Token.IsCancellationRequested)
            {
                //每30秒发送一次 心跳
                await SendHeartbeatAsync();
                await Task.Delay(30000, cts.Token);
            }
        }
        catch (Exception)
        {
            await Stop();
        }
    }

    private async Task SendHeartbeatAsync()
    {
        await SendSocketDataAsync(2, "[object Object]");
    }

    public async Task StartAsync()
    {
        await Console.Out.WriteLineAsync($"开始连接直播间 {roomId}");
        await Console.Out.WriteLineAsync("正在获取弹幕服务器地址");

        var queryParams = new Dictionary<string, string>
        {
            { "id", roomId.ToString() },
            { "type", "0" },
            { "web_location", "444.8" }
        };

        var url = CIDInfoUrl + roomId + "&wts=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (!string.IsNullOrEmpty(_sessionData))
        {
            await Console.Out.WriteLineAsync("已设置 SESSDATA 令牌，正在获取登录用户 ID");
            httpClient.DefaultRequestHeaders.Add("Cookie", $"SESSDATA={_sessionData};");
            var navResText = await httpClient.GetStringAsync("https://api.bilibili.com/x/web-interface/nav");
            var navRes = JsonNode.Parse(navResText);
            userId = navRes["data"]["mid"].GetValue<long>();
            await Console.Out.WriteLineAsync($"登录用户 ID: {userId}");
        }
        else
        {
            await Console.Out.WriteLineAsync("警告：未设置 SESSDATA 令牌，将以游客身份连接，无法获取完整弹幕发送者用户信息");
        }

        var (imgKey, subKey) = await BilibiliWbiSign.GetWbiKeys();
        queryParams = BilibiliWbiSign.EncWbi(queryParams, imgKey, subKey);
        var query = await new FormUrlEncodedContent(queryParams).ReadAsStringAsync();
        var requestUri = new Uri($"{CIDInfoUrl}?{query}");

        var resText = await httpClient.GetStringAsync(requestUri, cts.Token);
        await Console.Out.WriteLineAsync($"Bilibili {requestUri}: {resText}");
        var res = JsonNode.Parse(resText);
        token = res["data"]["token"].GetValue<string>();

        foreach (var host in res["data"]["host_list"]?.AsArray()!)
        {
            chatHost = host["host"]!.GetValue<string>();
            chatPort = host["port"]!.GetValue<int>();
            await Console.Out.WriteLineAsync($"尝试连接到 {chatHost}:{chatPort}");
            client = new TcpClient();
            try
            {
                await client.ConnectAsync(chatHost, chatPort);
                netStream = Stream.Synchronized(client.GetStream());

                await Console.Out.WriteLineAsync($"成功连接到 {chatHost}:{chatPort}");

                await Console.Out.WriteLineAsync("正在加入房间");
                await SendJoinChannel();

                var heartbeatLoop = HeartbeatLoop();
                var receiveMessageLoop = ReceiveMessageLoop();
                await Console.Out.WriteLineAsync("加入成功！正在接收弹幕信息");

                await Task.WhenAll(heartbeatLoop, receiveMessageLoop);
                return;
            }
            catch (Exception e)
            {
                await Console.Out.WriteLineAsync($"尝试连接到 {chatHost}:{chatPort} 时发生错误:\n {e}");
            }
        }
    }

    private async Task ReceiveMessageLoop()
    {
        var headerBuffer = new byte[16];

        while (!cts.Token.IsCancellationRequested && client.Connected)
            try
            {
                await netStream.ReadAllAsync(headerBuffer);
                var header = DanmakuProtocol.FromBuffer(headerBuffer);
                if (header.PacketLength < 16)
                {
                    await Console.Out.WriteLineAsync("数据包长度小于16");
                    continue;
                }

                var payloadLength = header.PacketLength - header.HeaderLength;
                if (payloadLength == 0) continue;
                var bodyBuffer = new byte[payloadLength];
                await netStream.ReadAllAsync(bodyBuffer);

                switch (header.Version)
                {
                    case DanmakuProtocol.ProtocolVersion.Normal:
                        var message = Encoding.UTF8.GetString(bodyBuffer);
                        // await Console.Out.WriteLineAsync("接收到报文: " + message);
                        _ = ProcessingMessage(message);
                        break;
                    case DanmakuProtocol.ProtocolVersion.Heartbeat:
                        break;
                    case DanmakuProtocol.ProtocolVersion.Deflate:
                        _ = ProcessingDefalteMessage(bodyBuffer);
                        break;
                    case DanmakuProtocol.ProtocolVersion.Brotli:
                        _ = ProcessingBrotliMessage(bodyBuffer);
                        break;
                }
            }
            catch (Exception e)
            {
                if (e is ObjectDisposedException)
                {
                    await Console.Out.WriteLineAsync("连接已释放");
                    break;
                }

                if (e is IOException)
                {
                    await Console.Out.WriteLineAsync("连接发生错误 " + e);
                    break;
                }

                await Console.Out.WriteLineAsync("接收消息时出现错误: " + e);
            }

        cts.Cancel();
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

        // Hex output 16 bytes
        //await Console.Out.WriteLineAsync(BitConverter.ToString(buffer.AsSpan(0, 16).ToArray()).Replace("-", " "));
        // Then output the rest
        //await Console.Out.WriteLineAsync(body);

        await netStream.WriteAsync(buffer, cts.Token);
    }

    private async Task ProcessingDefalteMessage(byte[] buffer)
    {
        using var output = new MemoryStream();
        using var input = new MemoryStream(buffer);
        await using var deflateStream = new DeflateStream(input, CompressionMode.Decompress);
        await deflateStream.CopyToAsync(output, cts.Token);
        var outputBuf = output.ToArray();

        // await Console.Out.WriteLineAsync("接收到 Deflate 报文: " + decompressed);

        await ProcessingDecompressedMessage(outputBuf);
    }

    private async Task ProcessingBrotliMessage(byte[] buffer)
    {
        using var output = new MemoryStream();
        using var input = new MemoryStream(buffer);
        await using var brotliStream = new BrotliStream(input, CompressionMode.Decompress);
        await brotliStream.CopyToAsync(output, cts.Token);
        var outputBuf = output.ToArray();

        await ProcessingDecompressedMessage(outputBuf);
    }

    private async Task ProcessingDecompressedMessage(byte[] buffer)
    {
        var stream = new MemoryStream(buffer);
        while (true)
        {
            if (stream.Position >= stream.Length) return;
            var headerBuffer = new byte[16];
            await stream.ReadAllAsync(headerBuffer, cts.Token);
            var header = DanmakuProtocol.FromBuffer(headerBuffer);

            if (header.PacketLength < 16)
            {
                await Console.Out.WriteLineAsync("数据包长度小于16");
                continue;
            }

            var payloadLength = header.PacketLength - header.HeaderLength;
            if (payloadLength == 0) continue;
            var bodyBuffer = new byte[payloadLength];
            await stream.ReadAllAsync(bodyBuffer, cts.Token);

            switch (header.Version)
            {
                case DanmakuProtocol.ProtocolVersion.Normal:
                    var message = Encoding.UTF8.GetString(bodyBuffer);
                    // await Console.Out.WriteLineAsync("接收到报文: " + message);
                    _ = ProcessingMessage(message);
                    break;
                case DanmakuProtocol.ProtocolVersion.Heartbeat:
                    break;
                case DanmakuProtocol.ProtocolVersion.Deflate:
                    _ = ProcessingDefalteMessage(bodyBuffer);
                    break;
                case DanmakuProtocol.ProtocolVersion.Brotli:
                    _ = ProcessingBrotliMessage(bodyBuffer);
                    break;
            }
        }
    }

    private async Task ProcessingMessage(string body)
    {
        try
        {
            var json = JsonNode.Parse(body);
            var cmd = json["cmd"].GetValue<string>();
            switch (cmd)
            {
                case "DANMU_MSG":
                {
                    // await Console.Out.WriteLineAsync("接收到弹幕: " + body);
                    var info = json["info"];
                    var msg = info[1].GetValue<string>();
                    var uid = info[2][0].GetValue<long>();
                    var uname = info[2][1].GetValue<string>();
                    await Console.Out.WriteLineAsync($"接收到弹幕: {uname} ({uid}): {msg}");
                    OnDanmakuMessage?.Invoke(this, new DammakuMessage
                    {
                        SenderUid = (uint)uid,
                        SenderName = uname,
                        Message = msg
                    });
                    break;
                }
            }
        }
        catch (Exception e)
        {
            // ignored
            await Console.Out.WriteLineAsync($"解析报文失败: {e}");
        }
    }

    private async Task SendJoinChannel()
    {
        // var packetModel = new JsonNode{ roomid = channelId, uid = 0, protover = 2, token = token, platform = "danmuji" };
        var payload = new JsonObject
        {
            ["roomid"] = roomId,
            ["uid"] = userId,
            ["protover"] = 3,
            ["key"] = token,
            ["platform"] = "web",
            ["type"] = 2
        }.ToJsonString();
        await SendSocketDataAsync(7, payload);
    }


    public static void Test()
    {
        var token = Environment.GetEnvironmentVariable("TOKEN");
        var crawer = new BilibiliLiveCommentCrawer(1338349, token);
        crawer.StartAsync().Wait();
        Console.Out.WriteLine("运行结束");
    }

    internal struct DanmakuProtocol
    {
        public int PacketLength;
        public short HeaderLength;
        public ProtocolVersion Version;
        public int Action;
        public int Parameter;

        public enum ProtocolVersion : short
        {
            Normal = 0,
            Heartbeat = 1,
            Deflate = 2,
            Brotli = 3
        }

        internal static DanmakuProtocol FromBuffer(byte[] buffer)
        {
            if (buffer.Length < 16) throw new ArgumentException();
            return new DanmakuProtocol
            {
                PacketLength = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(0, 4)),
                HeaderLength = BinaryPrimitives.ReadInt16BigEndian(buffer.AsSpan(4, 2)),
                Version = (ProtocolVersion)BinaryPrimitives.ReadInt16BigEndian(buffer.AsSpan(6, 2)),
                Action = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(8, 4)),
                Parameter = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(12, 4))
            };
        }

        public override string ToString()
        {
            return
                $"PacketLength: {PacketLength}, HeaderLength: {HeaderLength}, Version: {Version}, Action: {Action}, Parameter: {Parameter}";
        }
    }

    public record struct DammakuMessage
    {
        public string Message;
        public string SenderName;
        public uint SenderUid;
    }
}

internal static class ReadAllStream
{
    public static async Task ReadAllAsync(this Stream stream, byte[] buffer, CancellationToken token = default)
    {
        // await Console.Out.WriteLineAsync($"ReadAllAsync {buffer.Length} bytes");
        var offset = 0;
        var length = buffer.Length;
        while (length > 0)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, length), token);
            if (read == 0) throw new EndOfStreamException();
            offset += read;
            length -= read;
        }
    }
}