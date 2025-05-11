using Discord;
using Discord.WebSocket;
using HuTao.NET;

class Program
{
    // Discordとやりとりするためのクライアント
    private static DiscordSocketClient _discordClient = new DiscordSocketClient();

    // hoyolabからゲーム内情報を取得するクライアント
    // CookieV2は、ブラウザでhoyolabにログインし、開発者モードを開いて設定値を取得する
    private static Client _hutaoClient = new Client(
        new CookieV2()
        {
            // ltmid_v2 を設定
            ltmid_v2 = "",
            // ltoken_v2 を設定
            ltoken_v2 = "",
            // ltuid_v2 を設定
            ltuid_v2 = ""
        },
        new ClientData() { Language = "ja-jp" }
    );

    // botのトークン
    private static string Token = "";
    // 通知するチャンネルのID
    private static ulong ChannelId = 0;
    // 原神のユーザーID
    private static int GenshinUid = 0;

    // 1時間ごとのタイマーを設定する
    private static System.Timers.Timer _timer = new System.Timers.Timer(TimeSpan.FromHours(1).TotalMilliseconds);

    static async Task Main()
    {
        _discordClient.Log += Log;

        await _discordClient.LoginAsync(TokenType.Bot, Token);
        await _discordClient.StartAsync();

        // Botが準備完了したら、樹脂チェックイベントを実行する
        _discordClient.Ready += CheckResinEvent;

        await Task.Delay(Timeout.Infinite);
    }

    private static async Task CheckResinEvent()
    {
        // 一定時間おきに樹脂の状態を確認する
        _timer.Elapsed += async (sender, e) => await CheckResinAsync();
        _timer.Start();

        await Task.Delay(Timeout.Infinite);
    }

    private static async Task CheckResinAsync()
    {
        const int MaxResin = 200;

        try
        {
            // リアルタイムで更新されるデータを取得してくる
            var res = await _hutaoClient.FetchDailyNote(new GenshinUser(GenshinUid));

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

            string message2 = $"おい旅人！あと {FormatTimeSpan(res.data.ResinRecoveryTime)} で樹脂が溢れるぞ！";
                await SendMessageToDiscord(message2);
        }
        catch (Exception ex)
        {
            string message = $"おい旅人！想定外のエラーが発生したぞ！ {ex}";
            await SendMessageToDiscord(message);
        }
    }

    // Discordチャンネルにメッセージを送信
    private static async Task SendMessageToDiscord(string message)
    {
        var channel = _discordClient.GetChannel(ChannelId) as IMessageChannel;
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
