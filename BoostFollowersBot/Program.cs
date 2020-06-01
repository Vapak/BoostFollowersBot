using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Xml;
using InstagramApiSharp.API;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Logger;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Classes.SessionHandlers;
using InstagramApiSharp;

namespace BoostFollowersBot
{
    static class Program
    {
        private static TelegramBotClient Bot;

        private static IInstaApi InstaApi;

        public static async Task Main()
        {
            Bot = new TelegramBotClient(BotConfiguration.BotToken);

            InstaConnection();

            var me = await Bot.GetMeAsync();
            Console.Title = me.Username;            
            Bot.OnMessage += BotOnMessageReceived;
            Bot.OnMessageEdited += BotOnMessageReceived;

            Bot.StartReceiving(Array.Empty<UpdateType>());
            Console.WriteLine($"Start listening for @{me.Username}");

            Console.ReadLine();
            Bot.StopReceiving();
        }

        private async static void BotOnMessageReceived(object sender, MessageEventArgs e)
        {

            var message = e.Message;
            if (message == null || message.Type != MessageType.Text)
            {
                await Bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Я не понимаю тебя...",
                replyMarkup: new ReplyKeyboardRemove()
                );
                return;
            }

            switch (message.Text.Split(' ').First())
            {
                case "/help":
                    await Usage(message);
                    break;
                case "/wannafollows":
                    await SendListOfNicknames(message);
                    break;
                case "/casedone":
                    if (CaseList.GetUsername(message.Chat.Id) == "")                    
                        await SendUsernameMessage(message);                                               
                    else
                        await CheckFollowing(CaseList.GetUsername(message.Chat.Id), message.Chat.Id);
                    break;
                case "/insta":
                    CaseList.SetUsername(message.Chat.Id, message.Text.Split(' ')[1]);
                    await CheckFollowing(message.Text.Split(' ')[1], message.Chat.Id);
                    break;
                default:
                    await Bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Я не понимаю тебя...\n\n" +
                "/help - помощь",
                replyMarkup: new ReplyKeyboardRemove()
                );
                    return;

            }
        }

        private async static Task SendUsernameMessage(Message message)
        {            
            await Bot.SendTextMessageAsync(
               chatId: message.Chat.Id,
               text: "Отправь свой Instagram \n\n" +
               "Для этого напиши: \n/insta никнейм",
               replyMarkup: new ReplyKeyboardRemove()
           );
        }

        private async static Task SendListOfNicknames(Message message)
        {
            List<string> nicknames = CaseList.GetNicknames();
            string nicksMessage = null;
            foreach (var nick in nicknames)
            {
                nicksMessage += $"https://www.instagram.com/{nick.Trim()}/\n";
            }
            await Bot.SendTextMessageAsync(
                chatId: message.Chat.Id, 
                text: $"Подпишись на эти аккаунты:\n" +
                $"{nicksMessage}", 
                replyMarkup: new ReplyKeyboardRemove());

            CaseList.SaveCase(message.Chat.Id);
        }

        private async static Task Usage(Message message)
        {
            const string usage = "Usage:\n" +
                                        "/wannafollows - Ты получишь задание от меня\n" +
                                        "/help -  Если ты всё выполнил\n" +
                                        "/casedone - Помощь в работе со мной";
            await Bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: usage,
                replyMarkup: new ReplyKeyboardRemove()
            );
        } 

        private static async Task CheckFollowing(string nickname, long id)
        {
            try
            {
                List<string> nicknames = CaseList.GetNicknames(id.ToString());
                foreach (string nick in nicknames)
                {
                    var follows = await InstaApi.UserProcessor.GetUserFollowingAsync(nickname,
                    PaginationParameters.MaxPagesToLoad(10), searchQuery: nick);

                    if ((follows.Value.Count == 0)&&((nick != nickname)))
                    {
                        await Bot.SendTextMessageAsync(
                            chatId: id,
                            text: $"Похоже, ты не подписался на {nick}",
                            replyMarkup: new ReplyKeyboardRemove());
                        return;
                    }
                }
                CaseList.IncludeNickname(nickname, id);
                await Bot.SendTextMessageAsync(
                            chatId: id,
                            text: $"Поздравляю! Твоё имя внесено в список для задания! Жди новых подписчиков!",
                            replyMarkup: new ReplyKeyboardRemove());
            }
            catch(Exception ex)
            {
                await Bot.SendTextMessageAsync(
                            chatId: id,
                            text: $"Похоже, такого никнейма нет",
                            replyMarkup: new ReplyKeyboardRemove());
                Console.WriteLine(ex.Message);
                return;
            }
        }
        
        private static async void InstaConnection()
        {
            var userSession = new UserSessionData
            {
                UserName = "vapakzzz",
                Password = "bacugan99"
            };

            InstaApi = InstaApiBuilder.CreateBuilder()
                .SetUser(userSession)
                .UseLogger(new DebugLogger(LogLevel.Exceptions))
                .SetRequestDelay(RequestDelay.FromSeconds(0, 1))
                .SetSessionHandler(new FileSessionHandler() { FilePath = "state.bin" })
                .Build();
            Console.WriteLine("Bot Connecting...");
            //Load session
            try
            {
                InstaApi?.SessionHandler?.Load();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            if (!InstaApi.IsUserAuthenticated)
            {
                var delay = RequestDelay.FromSeconds(2, 2);
                delay.Disable();
                var logInResult = await InstaApi.LoginAsync();
                delay.Enable();
                //// Call this function before calling LoginAsync
                //await InstaApi.SendRequestsBeforeLoginAsync();
                //// wait 5 seconds
                //await Task.Delay(5000);
                Console.WriteLine(logInResult.Value);
                if (logInResult.Succeeded)
                {
                    Console.WriteLine("Bot Connected");

                    // Call this function after a successful login
                    await InstaApi.SendRequestsAfterLoginAsync();

                    // Save session 
                    InstaApi.SessionHandler.Save();
                }
                else
                {
                    Console.WriteLine("Connecting failed");
                    Console.ReadLine();
                }
            }
            else
            {
                Console.WriteLine("Bot alreadey Connected");
            }
        }
    }



}
