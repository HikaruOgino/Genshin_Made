using Discord;
using Discord.WebSocket;
using HuTao.NET;

class Program
{
    private static DiscordSocketClient _client = new DiscordSocketClient();

    // botのトークン
    private static string Token = ""; 
    // 通知するチャンネルのID
    private static ulong ChannelId = 0; 
    // 原神のユーザーID
    private static int GenshinUid = 0;

    static async Task Main()
    {
        _client.Log += Log;

        await _client.LoginAsync(TokenType.Bot, Token);
        await _client.StartAsync();

        // Botが準備完了したら、樹脂チェックイベントを実行する
        _client.Ready += CheckResinEvent;

        await Task.Delay(Timeout.Infinite);
    }

    private static async Task CheckResinEvent()
    {
        //  1時間ごとに樹脂の状態を確認
        var timer = new System.Timers.Timer(TimeSpan.FromHours(1).TotalMilliseconds);
        timer.Elapsed += (sender, e) => Task.Run(async () => await CheckResinAsync());
        timer.Start();

        await Task.Delay(Timeout.Infinite);
    }

    private static async Task CheckResinAsync()
    {
        const int MaxResin = 200;

        // ブラウザでhoyolabにログインし、開発者モードを開いて取得する
        CookieV2 cookieV2 = new CookieV2()
        {
            // ltmid_v2 を設定
            ltmid_v2 = "",
            // ltoken_v2 を設定
            ltoken_v2 = "", 
            // ltuid_v2 を設定
            ltuid_v2 = ""     
        };

        Client client = new Client(cookieV2, new ClientData() { Language = "ja-jp" });

        try
        {
            // リアルタイムで更新されるデータを取得してくる
            var res = await client.FetchDailyNote(new GenshinUser(GenshinUid));

            if (res.data == null)
            {
                string message = "おい旅人！原神のデータ取得に失敗したぞ！";
                await SendMessageToDiscord(message);
                return;
            }

            // 現在の天然樹脂が上限の80%以上になったら通知
            if (res.data.CurrentResin >= MaxResin * 0.8)
            {
                string message = $"おい旅人！あと {FormatTimeSpan(res.data.ResinRecoveryTime)} で樹脂が溢れるぞ！";
                await SendMessageToDiscord(message);
            }
        }
        catch (Exception ex)
        {
            string message = $"おい旅人！予想外のエラーが発生したぞ！ {ex}";
            await SendMessageToDiscord(message);
        }
    }

    // Discordチャンネルにメッセージを送信
    private static async Task SendMessageToDiscord(string message)
    {
        var channel = _client.GetChannel(ChannelId) as IMessageChannel;
        if (channel != null)
        {
            await channel.SendMessageAsync(message);
        }
    }

    private static string FormatTimeSpan(string milliseconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(Convert.ToInt64(milliseconds));
        string formattedTime = $"{time.Hours}時間{time.Minutes}分";
        return formattedTime;
    }

    private static Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }
}
