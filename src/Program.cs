using System.Text.Json;
using Discord;
using Discord.WebSocket;

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true
};

var token = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");
if (string.IsNullOrWhiteSpace(token))
{
    Console.WriteLine("DISCORD_BOT_TOKEN environment variable is not set.");
    return;
}

string? guildId;
if (args.Length > 0)
{
    guildId = args[0];
}
else
{
    guildId = Console.ReadLine();
}

if (string.IsNullOrWhiteSpace(guildId))
{
    Console.Error.WriteLine("Guild ID is not set.");
    return;
}

var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "tickets");

if (!Directory.Exists(folderPath))
{
    Directory.CreateDirectory(folderPath);
}
else
{
    Directory.Delete(folderPath, true);
    Directory.CreateDirectory(folderPath);
}

var client = new DiscordSocketClient();

client.Log += async (log) => await Console.Out.WriteLineAsync(log.Message);
client.Ready += async () =>
{
    var guild = client.GetGuild(ulong.Parse(guildId));
    if (guild is null)
    {
        Console.Error.WriteLine("Guild not found.");
        return;
    }

    var channels = guild.Channels.Where(x => x.Name.Contains("ticket", StringComparison.InvariantCultureIgnoreCase) || x.Name.Contains("closed", StringComparison.InvariantCultureIgnoreCase)).ToList();
    var downloadedChannels = 0;
    var totalChannels = channels.Count;

    foreach (var channel in channels)
    {
        if (channel is not ITextChannel textChannel)
        {
            downloadedChannels++;
            continue;
        }

        var filePath = Path.Combine(folderPath, $"{textChannel.Name}.json");
        var messages = new List<MessageDetails>();

        foreach (var message in await textChannel.GetMessagesAsync().FlattenAsync())
        {
            messages.Add(new MessageDetails(message.Author.Id, message.Author.Username, message.Content, message.CreatedAt));
        }

        var messagesJson = JsonSerializer.Serialize(messages, jsonOptions);
        await File.WriteAllTextAsync(filePath, messagesJson);

        downloadedChannels++;
        Console.WriteLine($"Downloaded: {downloadedChannels}/{totalChannels} channels.");
    }
};

await client.LoginAsync(TokenType.Bot, token);
await client.StartAsync();

await Task.Delay(-1);

record MessageDetails(ulong UserId, string UserName, string Content, DateTimeOffset CreationTime);
