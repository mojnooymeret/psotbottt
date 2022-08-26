using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace postbot
{
   internal class Program
   {
      private static TimerCallback callbackPost = new TimerCallback(CheckPost);
      private static TimerCallback callbackLoop = new TimerCallback(CheckLoop);
      private static string token { get; set; } = "5578584481:AAHyxYSnvgX6-X0T3NJusfxIQWBymPrNfhY";
      private static TelegramBotClient client;
      static void Main(string[] args)
      {
         client = new TelegramBotClient(token);
         client.StartReceiving();
         client.OnMessage += ClientMessage;
         client.OnUpdate += ClientUpdate;
         client.OnCallbackQuery += async (object sc, CallbackQueryEventArgs ev) => {
            InlineButtonOperation(sc, ev);
         };
         Timer time_1 = new Timer(callbackPost, null, 0, 15000);
         Timer time_2 = new Timer(callbackLoop, null, 0, 30000);
         Console.ReadLine();
      }

      private static void CheckPost(object state)
      {
         ConnectDB.LoadPost(posts);
         var now = DateTime.Now;
         for (int i = 0; i < posts.Count; i++) {
            if (posts[i].date.Split(' ')[0] == now.Date.ToString().Trim('{').Trim('}').Split(' ')[0] && posts[i].date.Split(' ')[1].Split(':')[0] == now.Hour.ToString() && posts[i].date.Split(' ')[1].Split(':')[1] == now.Minute.ToString()) {
               SendPost("PostOutNow♥" + posts[i].id, posts[i].id_chanel, "post");
            }
         }
      }

      private static void CheckLoop(object state)
      {
         try {
            ConnectDB.LoadLoop(loops);
            var now = DateTime.Now;
            for (int i = 0; i < loops.Count; i++) {
               if (loops[i].process == "start") {
                  double minutes = -100000;
                  try {
                     var next = Convert.ToDateTime(loops[i].last_send).AddMinutes(Convert.ToDouble(loops[i].time_interval));
                     minutes = (now - next).TotalMinutes;
                  } catch { minutes = 1; }
                  if (minutes > 0) {
                     string[] chanels = loops[i].id_chanels.Split('♥');
                     string idPost = string.Empty;
                     for (int j = 0; j < loops[i].posts.Split('♥').Length; j++) {
                        try {
                           if (loops[i].posts.Split('♥')[j].Length > 0) {
                              if (loops[i].posts.Split('♥')[j] == loops[i].last_post) { idPost = loops[i].posts.Split('♥')[j + 1]; break; }
                           }
                        } catch { break; }
                     }
                     if (idPost == string.Empty) idPost = loops[i].posts.Split('♥')[0];
                     ConnectDB.LoadPost(posts);
                     var post = posts.Find(x => x.id.ToString() == idPost);
                     for (int j = 0; j < chanels.Length; j++) {
                        try {
                           SendPost("SendPost♥" + post.id, chanels[j].Split('‼')[1], "loop");
                        } catch { }
                     }
                     ConnectDB.Query("update `Loop` set last_send = '" + now + "', last_post = '" + post.id + "' where id = " + loops[i].id + ";");
                  }
               }
            }
         } catch { }
      }

      public static List<User> users = new List<User>();
      public static List<Post> posts = new List<Post>();
      public static List<Loop> loops = new List<Loop>();

      private static async void ClientMessage(object sender, MessageEventArgs e)
      {
         var message = e.Message;
         if (message.Text == "/start") {
            ConnectDB.LoadUser(users);
            try {
               var user = users.Find(x => x.id_user == message.Chat.Id.ToString()).id_user;
            } catch {
               ConnectDB.Query("insert into `User` (id_user, chanels) values ('" + message.Chat.Id + "', 'none');");
               await client.SendTextMessageAsync(message.Chat.Id, "*Добро пожаловать*\n\nКоманды бота:\n/chanels - добавление канала и работа с уже подключенными\n/loops - создание цикличных постов и просмотр уже созданных", Telegram.Bot.Types.Enums.ParseMode.Markdown);
            }
         }
         else if (message.Text == "/chanels") {
            var keyborad = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Подключенные каналы", "ConnectChanels") }, new[] { InlineKeyboardButton.WithCallbackData("Добавить канал", "AddChanel") } });
            await client.SendTextMessageAsync(message.Chat.Id, "Менеджер каналов", replyMarkup: keyborad);
         }
         else if (message.Text == "/loops") {
            var msg = await client.SendTextMessageAsync(message.Chat.Id, "Загрузка");
            string[] loopArray = new string[0];
            ConnectDB.LoadLoop(loops);
            for (int i = 0; i < loops.Count; i++) {
               if (loops[i].id_user == message.Chat.Id.ToString()) {
                  Array.Resize(ref loopArray, loopArray.Length + 1);
                  loopArray[loopArray.Length - 1] = loops[i].name + "‼" + loops[i].id;
               }
            }
            var keyborad = GetInlineKeyboardLoops(loopArray);
            await client.EditMessageTextAsync(message.Chat.Id, msg.MessageId, "Ваши циклы", replyMarkup: keyborad);
         }
      }

      private static async void InlineButtonOperation(object sc, CallbackQueryEventArgs ev)
      {
         var message = ev.CallbackQuery.Message;
         var data = ev.CallbackQuery.Data;
         if (data == "AddChanel") {
            await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "*Добавление канала*\n\nЧтобы добавить канал в бота, пригласите его в требуемый канал со всеми правами администратора. После успешного добавления канала вам прийдет уведомление.", Telegram.Bot.Types.Enums.ParseMode.Markdown);
         }
         else if (data == "ConnectChanels") {
            ConnectDB.LoadUser(users);
            try {
               string[] chanels = users.Find(x => x.id_user == message.Chat.Id.ToString()).chanels.Split('♥');
               if (chanels[0] != "none") {
                  var keyboard = GetInlineKeyboardChanels(chanels);
                  await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "Ваши каналы:", replyMarkup: keyboard);
               }
               else {
                  var keyboardLoss = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Назад", "ChanelsMain") } });
                  await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "У вас нет подключенных каналов", replyMarkup: keyboardLoss);
               }
            } catch { }
         }
         else if (data == "ChanelsMain") {
            var keyborad = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Подключенные каналы", "ConnectChanels") }, new[] { InlineKeyboardButton.WithCallbackData("Добавить канал", "AddChanel") } });
            await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "Менеджер каналов", replyMarkup: keyborad);
         }
         else if (data.Contains("MoreChanel♥")) {
            try {
               var keyborad = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Новый пост", "NewPost♥" + data.Split('♥')[1]) }, new[] { InlineKeyboardButton.WithCallbackData("Отложенные посты", "WaitPost♥" + data.Split('♥')[1]) }, new[] { InlineKeyboardButton.WithCallbackData("Назад", "ConnectChanels") } });
               await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "Канал \"" + data.Split('♥')[1].Split('‼')[0] + "\"", replyMarkup: keyborad);
            } catch { }
         }
         else if (data.Contains("NewPost♥")) {
            ConnectDB.Query("insert into `Post` (id_user, id_chanel, status) values ('" + message.Chat.Id.ToString() + "', '" + data.Split('♥')[1].Split('‼')[1] + "', 'progress');");
            ConnectDB.LoadPost(posts);
            var postId = posts.Find(x => x.id_user == message.Chat.Id.ToString() && x.id_chanel == data.Split('♥')[1].Split('‼')[1] && x.status == "progress");
            await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "*Добавление поста в канал \"" + data.Split('♥')[1].Split('‼')[0] + "\"*\n\n*текстовое содержимое*\nВведите текстовое содержимое поста (текст, emoji, стикер)", Telegram.Bot.Types.Enums.ParseMode.Markdown); ;
            int s = 0;
            while (true) {
               try {
                  s++;
                  var updates = await client.GetUpdatesAsync(s);
                  for (int i = 0; i < updates.Length; i++) {
                     if (updates[i].Message.From.Id == message.Chat.Id) {
                        if (updates[i].Message.Type == Telegram.Bot.Types.Enums.MessageType.Text || updates[i].Message.Type == Telegram.Bot.Types.Enums.MessageType.Sticker) {
                           if (updates[i].Message.Text == "/start" || updates[i].Message.Text == "/chanels" || updates[i].Message.Text == "/loops") { ConnectDB.Query("delete from `Post` where id = " + postId.id + ";"); return; }
                           string text = updates[i].Message.Text;
                           var keyborad = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Пропустить", "SkipMedia♥" + postId.id) } });
                           try {
                              await client.SendTextMessageAsync(message.Chat.Id, "*Добавление поста в канал \"" + data.Split('♥')[1].Split('‼')[0] + "\"*\n\n*Медиа*\nВставьте медиа содержимое приветствия одним сообщением", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: keyborad);
                           } catch { await client.SendTextMessageAsync(message.Chat.Id, "*Ошибка*\n\nДанный тип текста не поддерживается, возможно вы ввели недопустимые emoji.\nВведите текст еще раз.", Telegram.Bot.Types.Enums.ParseMode.Markdown); break; }
                           while (true) {
                              s++;
                              updates = await client.GetUpdatesAsync(s);
                              string media = string.Empty;
                              for (int j = 0; j < updates.Length; j++) {
                                 try {
                                    if (updates[j].Message.Type == Telegram.Bot.Types.Enums.MessageType.Document || updates[j].Message.Type == Telegram.Bot.Types.Enums.MessageType.Audio || updates[j].Message.Type == Telegram.Bot.Types.Enums.MessageType.Photo || updates[j].Message.Type == Telegram.Bot.Types.Enums.MessageType.Video) {
                                       ConnectDB.Query("update `Post` set media = 'none', status = 'preview' where id = " + postId.id + ";");
                                       if (updates[j].Message.Photo != null) media += updates[j].Message.Type + "|" + updates[j].Message.Photo[updates[j].Message.Photo.Length - 1].FileId + "‼";
                                       else if (updates[j].Message.Video != null) media += updates[j].Message.Type + "|" + updates[j].Message.Video.FileId + "‼";
                                       else if (updates[j].Message.Document != null) media += updates[j].Message.Type + "|" + updates[j].Message.Document.FileId + "‼";
                                       else if (updates[j].Message.Audio != null) media += updates[j].Message.Type + "|" + updates[j].Message.Audio.FileId + "‼";
                                    }
                                    else if (updates[i].Message.Text == "/start" || updates[i].Message.Text == "/chanels" || updates[i].Message.Text == "/loops") { ConnectDB.Query("delete from `Post` where id = " + postId.id + ";"); return; }
                                 } catch { }
                              }
                              media = media.Trim('‼');
                              ConnectDB.LoadPost(posts);
                              var status = posts.Find(x => x.id_user == message.Chat.Id.ToString() && x.id_chanel == data.Split('♥')[1].Split('‼')[1] && x.id == postId.id).status;
                              if (status == "preview") {
                                 var keyboradPreview = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Выложить сейчас", "PostOutNow♥" + postId.id + "♥" + postId.id_chanel) }, new[] { InlineKeyboardButton.WithCallbackData("Отложить", "DatePost♥" + postId.id) } });
                                 ConnectDB.Query("update `Post` set text = '" + text + "', media = '" + media + "' where id = " + postId.id + ";");
                                 if (media.Contains('‼')) {
                                    string[] medias = media.Split('‼');
                                    IAlbumInputMedia[] mediaGroup = GetMedia(medias, text);
                                    await client.SendMediaGroupAsync(message.Chat.Id, mediaGroup);
                                 }
                                 else if (media != string.Empty && media != "") {
                                    if (media.Split('|')[0] == "Photo") await client.SendPhotoAsync(message.Chat.Id, media.Split('|')[1], caption: text);
                                    if (media.Split('|')[0] == "Video") await client.SendVideoAsync(message.Chat.Id, media.Split('|')[1], caption: text);
                                    if (media.Split('|')[0] == "Document") await client.SendDocumentAsync(message.Chat.Id, media.Split('|')[1], caption: text);
                                    if (media.Split('|')[0] == "Audio") await client.SendAudioAsync(message.Chat.Id, media.Split('|')[1], caption: text);
                                 }
                                 else await client.SendTextMessageAsync(message.Chat.Id, text, Telegram.Bot.Types.Enums.ParseMode.Markdown);
                                 await client.SendTextMessageAsync(message.Chat.Id, "Пост сформирован, что требуется сделать?", replyMarkup: keyboradPreview);
                                 return;
                              }
                           }
                        }
                     }
                  }
               } catch { }
            }
         }
         else if (data.Contains("SkipMedia♥")) {
            await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
            ConnectDB.Query("update `Post` set media = 'none', status = 'preview' where id = " + data.Split('♥')[1] + ";");
         }
         else if (data.Contains("PostOutNow♥")) {
            await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
            SendPost(data, data.Split('♥')[2], "post");
            await client.SendTextMessageAsync(message.Chat.Id, "Пост успешно отправлен");
         }
         else if (data.Contains("DatePost♥")) {
            await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "Введите дату и время (МСК), в которое требуется выложить пост\n\n(*ОБЯЗАТЕЛЬНО* в таком формате: 02.02.2022 22:20)", Telegram.Bot.Types.Enums.ParseMode.Markdown);
            int s = 0;
            while (true) {
               s++;
               var updates = await client.GetUpdatesAsync(s);
               for (int i = 0; i < updates.Length; i++) {
                  try {
                     if (updates[i].Message.From.Id == message.Chat.Id) {
                        if (updates[i].Message.Type == Telegram.Bot.Types.Enums.MessageType.Text || updates[i].Message.Type == Telegram.Bot.Types.Enums.MessageType.Sticker) {
                           if (updates[i].Message.Text == "/start" || updates[i].Message.Text == "/chanels" || updates[i].Message.Text == "/loops") { ConnectDB.Query("delete from `Post` where id = " + data.Split('♥')[1] + ";"); return; }
                           string text = updates[i].Message.Text;
                           if (text.Contains('.') && text.Contains(':') && text.Contains(" ")) {
                              if (text.Split(' ').Length == 2 && text.Split(':').Length == 2 && text.Split('.').Length == 3) {
                                 try {
                                    var time = Convert.ToDateTime(text);
                                    ConnectDB.Query("update `Post` set date = '" + time + "' where id = " + data.Split('♥')[1] + ";");
                                    await client.SendTextMessageAsync(message.Chat.Id, "Пост отложен на " + time);
                                    return;
                                 } catch { await client.SendTextMessageAsync(message.Chat.Id, "Неверный формат даты\nВведите дату еще раз (Пример: 02.02.2022 10:00)"); }
                              }
                           }
                        }
                     }
                  } catch { }
               }
            }
         }
         else if (data.Contains("WaitPost♥")) {
            WaitPosts(message, data);
         }
         else if (data.Contains("MorePost♥")) {
            try {
               ConnectDB.LoadPost(posts);
               var post = posts.Find(x => x.id.ToString() == data.Split('♥')[1].Split('‼')[2]);
               var keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Предпросмотр", "PreviewPost♥" + data.Split('♥')[1]) }, new[] { InlineKeyboardButton.WithCallbackData("Изменить дату", "DatePost♥" + post.id) }, new[] { InlineKeyboardButton.WithCallbackData("Выложить сейчас", "SendPost♥" + post.id + "♥" + post.id_chanel) }, new[] { InlineKeyboardButton.WithCallbackData("Удалить", "DelPost♥" + post.id) } });
               await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "Пост на " + data.Split('♥')[1].Split('‼')[1], replyMarkup: keyboard);
            } catch { }
         }
         else if (data.Contains("PreviewPost♥")) {
            try {
               await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
               GetPost(data.Split('♥')[1].Split('‼')[2], message);
               ConnectDB.LoadPost(posts);
               var post = posts.Find(x => x.id.ToString() == data.Split('♥')[1].Split('‼')[2]);
               var keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Предпросмотр", "PreviewPost♥" + data.Split('♥')[1]) }, new[] { InlineKeyboardButton.WithCallbackData("Изменить дату", "DatePost♥" + post.id) }, new[] { InlineKeyboardButton.WithCallbackData("Выложить сейчас", "SendPost♥" + post.id) }, new[] { InlineKeyboardButton.WithCallbackData("Удалить", "DelPost♥" + post.id) } });
               await client.SendTextMessageAsync(message.Chat.Id, "Пост на " + data.Split('♥')[1].Split('‼')[1], replyMarkup: keyboard);
            } catch { }
         }
         else if (data.Contains("DelPost♥")) {
            try {
               ConnectDB.Query("delete from `Post` where id = " + data.Split('♥')[1] + ";");
               await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "Пост успешно удален");
            } catch { }
         }
         else if (data.Contains("SendPost♥")) {
            try {
               SendPost(data, data.Split('♥')[2], "post");
               await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "Пост успешно отправлен");
            } catch { }
         }
         else if (data.Contains("StartLoop♥")) {
            ConnectDB.Query("update `Loop` set process = 'start' where id = " + data.Split('♥')[1] + ";");
            await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "Цикл запущен");
         }
         else if (data.Contains("StopLoop♥")) {
            ConnectDB.Query("update `Loop` set process = 'stop', last_send = '', last_post = '' where id = " + data.Split('♥')[1] + ";");
            await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "Цикл остановлен");
         }
         else if (data.Contains("DelLoop♥")) {
            try {
               ConnectDB.LoadLoop(loops);
               var loop = loops.Find(x => x.id.ToString() == data.Split('♥')[1]);
               string[] posts = loop.posts.Split('♥');
               string request = "delete from `Loop` where id = " + data.Split('♥')[1];
               for (int i = 0; i < posts.Length; i++) if (posts[i] != "" && posts[i] != null && posts[i] != string.Empty) request += "delete from `Post` where id = " + posts[i] + ";\n";
               ConnectDB.Query(request);
               await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "Цикл " + loop.name + " успешно удален");
            } catch { }
         }
         else if (data.Contains("MoreLoop♥")) {
            ConnectDB.LoadLoop(loops);
            try {
               var loop = loops.Find(x => x.id.ToString() == data.Split('♥')[1].Split('‼')[1]);
               if (loop.process == "start") {
                  var keyborad = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Остановить цикл", "StopLoop♥" + loop.id) }, new[] { InlineKeyboardButton.WithCallbackData("Удалить цикл", "DelLoop♥" + loop.id) } });
                  await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "Цикл " + loop.name, replyMarkup: keyborad);
               }
               else if (loop.process == "stop") {
                  var keyborad = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Запустить цикл", "StartLoop♥" + loop.id) }, new[] { InlineKeyboardButton.WithCallbackData("Удалить цикл", "DelLoop♥" + loop.id) } });
                  await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "Цикл " + loop.name, replyMarkup: keyborad);
               }
            } catch { }
         }
         else if (data == "AddLoop") {
            ConnectDB.Query("insert into `Loop` (id_user, time_interval) values ('" + message.Chat.Id.ToString() + "', 'none');");
            ConnectDB.LoadLoop(loops);
            var loop = loops.FindAll(x => x.id_user == message.Chat.Id.ToString());
            var thisLoop = loop[loop.Count - 1];
            var msg = await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "*Добавление цикла*\n\nУкажите наименование цикла", Telegram.Bot.Types.Enums.ParseMode.Markdown);
            int s = 0;
            try {
               while (true) {
                  s++;
                  var updates = await client.GetUpdatesAsync(s);
                  for (int i = 0; i < updates.Length; i++) {
                     if (updates[i].Message.From.Id == message.Chat.Id) {
                        if (updates[i].Message.Type == Telegram.Bot.Types.Enums.MessageType.Text) {
                           if (updates[i].Message.Text == "/start" || updates[i].Message.Text == "/chanels" || updates[i].Message.Text == "/loops") { ConnectDB.Query("delete from `Loop` where id = " + thisLoop.id + ";"); return; }
                           string name = updates[i].Message.Text;
                           ConnectDB.Query("update `Loop` set name = '" + name + "' where id = " + thisLoop.id + ";");
                           ConnectDB.LoadUser(users);
                           try {
                              var chanels = users.Find(x => x.id_user == message.Chat.Id.ToString()).chanels;
                              string[] chanelArray = chanels.Split('♥');
                              var keyboard = GetInlineKeyboardChanelsLoop(chanelArray, thisLoop.id.ToString());
                              msg = await client.SendTextMessageAsync(message.Chat.Id, "Загузка");
                              await client.EditMessageTextAsync(message.Chat.Id, msg.MessageId, "*Выбор каналов*\n\nВыберите каналы, в которые будут выкладываться посты", replyMarkup: keyboard, parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                              while (true) {
                                 try {
                                    var status = loops.Find(x => x.id == thisLoop.id);
                                    if (status.time_interval == "next") {
                                       CreateLoopProcess(thisLoop.id.ToString(), message);
                                       break;
                                    }
                                 } catch { }
                              }
                           } catch { return; }

                        }
                        else {
                           await client.DeleteMessageAsync(message.Chat.Id, msg.MessageId);
                           await client.SendTextMessageAsync(message.Chat.Id, "Неверный формат наименования, введите имя цикла еще раз (текст)");
                        }
                     }
                  }
               }
            } catch { }
         }
         else if (data.Contains("AddLoopChanel♥")) {
            ConnectDB.LoadLoop(loops);
            var loop = loops.Find(x => x.id.ToString() == data.Split('♥')[1].Split('‼')[2]);
            if (loop.id_chanels == null || loop.id_chanels == string.Empty || loop.id_chanels == "") ConnectDB.Query("update `Loop` set id_chanels = '" + data.Split('♥')[1].Split('‼')[0] + "‼" + data.Split('♥')[1].Split('‼')[1] + "' where id = " + loop.id + ";");
            else ConnectDB.Query("update `Loop` set id_chanels = '" + loop.id_chanels + "♥" + data.Split('♥')[1].Split('‼')[0] + "‼" + data.Split('♥')[1].Split('‼')[1] + "' where id = " + loop.id + ";");
            await client.SendTextMessageAsync(message.Chat.Id, "Канал \"" + data.Split('♥')[1].Split('‼')[0] + "\" добавлен в цикл");
         }
         else if (data.Contains("NextLoop♥")) {
            try {
               ConnectDB.LoadLoop(loops);
               var loop = loops.Find(x => x.id.ToString() == data.Split('♥')[1]).id_chanels;
               if (loop != null && loop != string.Empty && loop != "") { ConnectDB.Query("update `Loop` set time_interval = 'next' where id = " + data.Split('♥')[1] + ";"); ConnectDB.LoadLoop(loops); }
               else await client.SendTextMessageAsync(message.Chat.Id, "Добавьте как минимум один канал в цикл");
            } catch { }
         }
         else if (data.Contains("AddLoopPost♥")) {
            ConnectDB.Query("insert into `Post` (id_user, status) values ('" + message.Chat.Id.ToString() + "', 'progress');");
            ConnectDB.LoadPost(posts);
            var postId = posts.Find(x => x.id_user == message.Chat.Id.ToString() && x.status == "progress").id;
            await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "*Добавление поста в цикл*\n\n*текстовое содержимое*\nВведите текстовое содержимое поста (текст, emoji, стикер)", Telegram.Bot.Types.Enums.ParseMode.Markdown); ;
            int s = 0;
            try {
               while (true) {
                  s++;
                  var updates = await client.GetUpdatesAsync(s);
                  for (int i = 0; i < updates.Length; i++) {
                     if (updates[i].Message.From.Id == message.Chat.Id) {
                        if (updates[i].Message.Type == Telegram.Bot.Types.Enums.MessageType.Text || updates[i].Message.Type == Telegram.Bot.Types.Enums.MessageType.Sticker) {
                           if (updates[i].Message.Text == "/start" || updates[i].Message.Text == "/chanels" || updates[i].Message.Text == "/loops") { ConnectDB.Query("delete from `Post` where id = " + postId + ";"); return; }
                           string text = updates[i].Message.Text;
                           var keyborad = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Пропустить", "SkipMedia♥" + postId) } });
                           try {
                              await client.SendTextMessageAsync(message.Chat.Id, "*Добавление поста в цикл*\n\n*Медиа*\nВставьте медиа содержимое приветствия одним сообщением", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: keyborad);
                           } catch { await client.SendTextMessageAsync(message.Chat.Id, "*Ошибка*\n\nДанный тип текста не поддерживается, возможно вы ввели недопустимые emoji.\nВведите текст еще раз.", Telegram.Bot.Types.Enums.ParseMode.Markdown); break; }
                           while (true) {
                              s++;
                              updates = await client.GetUpdatesAsync(s);
                              string media = string.Empty;
                              for (int j = 0; j < updates.Length; j++) {
                                 try {
                                    if (updates[j].Message.Type == Telegram.Bot.Types.Enums.MessageType.Document || updates[j].Message.Type == Telegram.Bot.Types.Enums.MessageType.Audio || updates[j].Message.Type == Telegram.Bot.Types.Enums.MessageType.Photo || updates[j].Message.Type == Telegram.Bot.Types.Enums.MessageType.Video) {
                                       ConnectDB.Query("update `Post` set media = 'none', status = 'preview' where id = " + postId + ";");
                                       if (updates[j].Message.Photo != null) media += updates[j].Message.Type + "|" + updates[j].Message.Photo[updates[j].Message.Photo.Length - 1].FileId + "‼";
                                       else if (updates[j].Message.Video != null) media += updates[j].Message.Type + "|" + updates[j].Message.Video.FileId + "‼";
                                       else if (updates[j].Message.Document != null) media += updates[j].Message.Type + "|" + updates[j].Message.Document.FileId + "‼";
                                       else if (updates[j].Message.Audio != null) media += updates[j].Message.Type + "|" + updates[j].Message.Audio.FileId + "‼";
                                    }
                                    else if (updates[i].Message.Text == "/start" || updates[i].Message.Text == "/chanels" || updates[i].Message.Text == "/loops") { ConnectDB.Query("delete from `Post` where id = " + postId + ";"); return; }
                                    media = media.Trim('‼');
                                 } catch { }
                              }
                              ConnectDB.LoadPost(posts);
                              var status = posts.Find(x => x.id == postId).status;
                              if (status == "preview") {
                                 var keyboradPreview = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Сохранить пост в цикле", "SavePostLoop♥" + postId + "‼" + data.Split('♥')[1]) }, new[] { InlineKeyboardButton.WithCallbackData("Отменить", "CancelPostLoop♥" + postId) } });
                                 ConnectDB.Query("update `Post` set text = '" + text + "', media = '" + media + "' where id = " + postId + ";");
                                 if (media.Contains('‼')) {
                                    string[] medias = media.Split('‼');
                                    IAlbumInputMedia[] mediaGroup = GetMedia(medias, text);
                                    await client.SendMediaGroupAsync(message.Chat.Id, mediaGroup);
                                 }
                                 else if (media != string.Empty && media != "") {
                                    if (media.Split('|')[0] == "Photo") await client.SendPhotoAsync(message.Chat.Id, media.Split('|')[1], caption: text);
                                    if (media.Split('|')[0] == "Video") await client.SendVideoAsync(message.Chat.Id, media.Split('|')[1], caption: text);
                                    if (media.Split('|')[0] == "Document") await client.SendDocumentAsync(message.Chat.Id, media.Split('|')[1], caption: text);
                                    if (media.Split('|')[0] == "Audio") await client.SendAudioAsync(message.Chat.Id, media.Split('|')[1], caption: text);
                                 }
                                 else await client.SendTextMessageAsync(message.Chat.Id, text, Telegram.Bot.Types.Enums.ParseMode.Markdown);
                                 await client.SendTextMessageAsync(message.Chat.Id, "Пост для цикла сформирован, что требуется сделать?", replyMarkup: keyboradPreview);
                                 return;
                              }
                           }
                        }
                     }
                  }
               }
            } catch { }
         }
         else if (data.Contains("SavePostLoop♥")) {
            ConnectDB.LoadLoop(loops);
            var loop = loops.Find(x => x.id.ToString() == data.Split('♥')[1].Split('‼')[1]).posts;
            if (loop == null || loop == string.Empty || loop == "") ConnectDB.Query("update `Loop` set posts = '" + data.Split('♥')[1].Split('‼')[0] + "' where id = " + data.Split('♥')[1].Split('‼')[1] + ";");
            else ConnectDB.Query("update `Loop` set posts = '" + loop + "♥" + data.Split('♥')[1].Split('‼')[0] + "' where id = " + data.Split('♥')[1].Split('‼')[1] + ";");
            await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "Пост успешно добавлен цикл");
            CreateLoopProcess(data.Split('♥')[1].Split('‼')[1], message);
         }
         else if (data.Contains("CancelPostLoop♥")) {
            ConnectDB.Query("delete from `Post` where id = " + data.Split('♥')[1]);
         }
         else if (data.Contains("NextLoopInterval♥")) {
            ConnectDB.Query("update `Loop` set time_interval = 'end', process = 'stop' where id = " + data.Split('♥')[1] + ";");
            CreateLoopProcess(data.Split('♥')[1], message);
         }
      }

      private static async void ClientUpdate(object sender, UpdateEventArgs e)
      {
         try {
            var update = e.Update;
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.MyChatMember && update.MyChatMember.NewChatMember.Status == Telegram.Bot.Types.Enums.ChatMemberStatus.Administrator) {
               var me = await client.GetMeAsync();
               if (me.Id == update.MyChatMember.NewChatMember.User.Id) {
                  ConnectDB.LoadUser(users);
                  string chanel = update.MyChatMember.Chat.Title + "‼" + update.MyChatMember.Chat.Id;
                  string actual = string.Empty;
                  try {
                     actual = users.Find(x => x.id_user == update.MyChatMember.From.Id.ToString()).chanels;
                     if (!actual.Contains(update.MyChatMember.Chat.Id.ToString())) {
                        if (actual != string.Empty || actual != "" && actual != null) {
                           if (actual != "none") chanel = actual + "♥" + chanel;
                           ConnectDB.Query("update `User` set chanels = '" + chanel + "' where id_user = " + update.MyChatMember.From.Id + ";");
                           string username = update.MyChatMember.Chat.Username;
                           if (username == null) await client.SendTextMessageAsync(update.MyChatMember.From.Id, "Вы успешно добавили канал \"" + update.MyChatMember.Chat.Title + "\"");
                           else await client.SendTextMessageAsync(update.MyChatMember.From.Id, "Вы успешно добавили канал @" + username);
                        }
                     }
                  } catch { }
               }
            }
            else if (update.Type == Telegram.Bot.Types.Enums.UpdateType.MyChatMember && update.MyChatMember.NewChatMember.Status == Telegram.Bot.Types.Enums.ChatMemberStatus.Kicked) {
               var me = await client.GetMeAsync();
               if (me.Id == update.MyChatMember.NewChatMember.User.Id) {
                  ConnectDB.LoadUser(users);
                  string actual = string.Empty;
                  try {
                     actual = users.Find(x => x.id_user == update.MyChatMember.From.Id.ToString()).chanels;
                     if (actual.Contains('♥')) {
                        string[] chanels = actual.Split('♥');
                        string newChanel = string.Empty;
                        for (int i = 0; i < chanels.Length; i++) { if (chanels[i].Split('‼')[1] != update.MyChatMember.Chat.Id.ToString()) newChanel += chanels[i] + "♥"; }
                        newChanel = newChanel.Trim('♥');
                        ConnectDB.Query("update `User` set chanels = '" + newChanel + "' where id_user = " + update.MyChatMember.From.Id + ";");
                        string username = update.MyChatMember.Chat.Username;
                        if (username == null) await client.SendTextMessageAsync(update.MyChatMember.From.Id, "Вы успешно удалили канал \"" + update.MyChatMember.Chat.Title + "\"");
                        else await client.SendTextMessageAsync(update.MyChatMember.From.Id, "Вы успешно удалили канал @" + username);
                     }
                     else {
                        ConnectDB.Query("update `User` set chanels = 'none' where id_user = " + update.MyChatMember.From.Id + ";");
                        string username = update.MyChatMember.Chat.Username;
                        if (username == null) await client.SendTextMessageAsync(update.MyChatMember.From.Id, "Вы успешно удалили канал \"" + update.MyChatMember.Chat.Title + "\"");
                        else await client.SendTextMessageAsync(update.MyChatMember.From.Id, "Вы успешно удалили канал @" + username);
                     }
                  } catch { }
               }
            }
         } catch { }
      }

      private static async void CreateLoopProcess(string id, Message message)
      {
         try {
            ConnectDB.LoadLoop(loops);
            var status = loops.Find(x => x.id.ToString() == id);
            if (status.time_interval != "end") {
               string[] allPost;
               if (status.posts != null && status.posts != string.Empty && status.posts != "") allPost = status.posts.Split('♥');
               else allPost = new string[0];
               var kbrd = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Добавить пост", "AddLoopPost♥" + status.id) }, new[] { InlineKeyboardButton.WithCallbackData("Далее", "NextLoopInterval♥" + status.id) } });
               await client.SendTextMessageAsync(message.Chat.Id, "Количество постов в цикле: " + allPost.Length, replyMarkup: kbrd);
            }
            else {
               await client.SendTextMessageAsync(message.Chat.Id, "*Интервал между постами*\n\nУкажите временной интервал между публикациями постов в минутах", Telegram.Bot.Types.Enums.ParseMode.Markdown);
               int s = 1000;
               while (true) {
                  s++;
                  var updates = await client.GetUpdatesAsync(s);
                  for (int i = 0; i < updates.Length; i++) {
                     if (updates[i].Message.From.Id == message.Chat.Id) {
                        if (updates[i].Message.Type == Telegram.Bot.Types.Enums.MessageType.Text) {
                           if (updates[i].Message.Text == "/start" || updates[i].Message.Text == "/chanels" || updates[i].Message.Text == "/loops") { ConnectDB.Query("delete from `Loop` where id = " + id + ";"); return; }
                           try {
                              int time = Convert.ToInt32(updates[i].Message.Text);
                              ConnectDB.Query("update `Loop` set time_interval = '" + time.ToString() + "' where id = " + id + ";");
                              await client.SendTextMessageAsync(message.Chat.Id, "Цикл готов, вы можете запустить его в /loops - Цикл");
                              return;
                           } catch { await client.SendTextMessageAsync(message.Chat.Id, "Неверный формат\nВведите временной инетервал между публикациями постов в минутах"); break; }
                        }
                     }
                  }
               }
            }
         } catch { }
      }

      private static async void WaitPosts(Message message, string data)
      {
         ConnectDB.LoadPost(posts);
         try {
            string[] post = new string[0];
            for (int i = 0; i < posts.Count; i++) {
               if (posts[i].id_user == message.Chat.Id.ToString() && posts[i].id_chanel == data.Split('♥')[1].Split('‼')[1]) {
                  Array.Resize(ref post, post.Length + 1);
                  post[post.Length - 1] = posts[i].id_chanel + "‼" + posts[i].date + "‼" + posts[i].id;
               }
            }
            var keyboard = GetInlineKeyboardPosts(post, data.Split('♥')[1]);
            await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "Отложенные посты канала \"" + data.Split('♥')[1].Split('‼')[0] + "\"", replyMarkup: keyboard);
         } catch { }
      }

      private static async void SendPost(string data, string id_chanel, string type)
      {
         try {
            ConnectDB.LoadPost(posts);
            var post = posts.Find(x => x.id.ToString() == data.Split('♥')[1]);
            if (post.media.Contains('‼')) {
               string[] medias = post.media.Split('‼');
               IAlbumInputMedia[] mediaGroup = GetMedia(medias, post.text);
               await client.SendMediaGroupAsync(id_chanel, mediaGroup);
            }
            else if (post.media != string.Empty && post.media != "" && post.media != "none") {
               if (post.media.Split('|')[0] == "Photo") await client.SendPhotoAsync(id_chanel, post.media.Split('|')[1], caption: post.text);
               else if (post.media.Split('|')[0] == "Video") await client.SendVideoAsync(id_chanel, post.media.Split('|')[1], caption: post.text);
               else if (post.media.Split('|')[0] == "Document") await client.SendDocumentAsync(id_chanel, post.media.Split('|')[1], caption: post.text);
               else if (post.media.Split('|')[0] == "Audio") await client.SendAudioAsync(id_chanel, post.media.Split('|')[1], caption: post.text);
            }
            else {
               await client.SendTextMessageAsync(id_chanel, post.text);
            }
            if (type == "post") ConnectDB.Query("delete from `Post` where id = " + data.Split('♥')[1]);
         } catch { }
      }

      private static async void GetPost(string id, Message message)
      {
         try {
            ConnectDB.LoadPost(posts);
            var post = posts.Find(x => x.id.ToString() == id);
            if (post.media.Contains('‼')) {
               string[] medias = post.media.Split('‼');
               IAlbumInputMedia[] mediaGroup = GetMedia(medias, post.text);
               await client.SendMediaGroupAsync(message.Chat.Id, mediaGroup);
            }
            else if (post.media != string.Empty && post.media != "" && post.media != "none") {
               if (post.media.Split('|')[0] == "Photo") await client.SendPhotoAsync(message.Chat.Id, post.media.Split('|')[1], caption: post.text);
               else if (post.media.Split('|')[0] == "Video") await client.SendVideoAsync(message.Chat.Id, post.media.Split('|')[1], caption: post.text);
               else if (post.media.Split('|')[0] == "Document") await client.SendDocumentAsync(message.Chat.Id, post.media.Split('|')[1], caption: post.text);
               else if (post.media.Split('|')[0] == "Audio") await client.SendAudioAsync(message.Chat.Id, post.media.Split('|')[1], caption: post.text);
            }
            else {
               await client.SendTextMessageAsync(message.Chat.Id, post.text);
            }
         } catch { }
      }

      private static InlineKeyboardButton[][] GetInlineKeyboardChanels(string[] stringArray)
      {
         var keyboardInline = new InlineKeyboardButton[stringArray.Length + 1][];
         for (int i = 0; i < stringArray.Length + 1; i++) {
            var keyboardButtons = new InlineKeyboardButton[1];
            if (i + 1 != stringArray.Length + 1) {
               keyboardButtons[0] = new InlineKeyboardButton {
                  Text = stringArray[i].Split('‼')[0],
                  CallbackData = "MoreChanel♥" + stringArray[i],
               };
            }
            else {
               keyboardButtons[0] = new InlineKeyboardButton {
                  Text = "Назад",
                  CallbackData = "ChanelsMain",
               };
            }
            keyboardInline[i] = keyboardButtons;
         }
         return keyboardInline;
      }

      private static InlineKeyboardButton[][] GetInlineKeyboardChanelsLoop(string[] stringArray, string id)
      {
         var keyboardInline = new InlineKeyboardButton[stringArray.Length + 1][];
         for (int i = 0; i < stringArray.Length + 1; i++) {
            var keyboardButtons = new InlineKeyboardButton[1];
            if (i + 1 != stringArray.Length + 1) {
               keyboardButtons[0] = new InlineKeyboardButton {
                  Text = stringArray[i].Split('‼')[0],
                  CallbackData = "AddLoopChanel♥" + stringArray[i] + "‼" + id,
               };
            }
            else {
               keyboardButtons[0] = new InlineKeyboardButton {
                  Text = "Далее",
                  CallbackData = "NextLoop♥" + id,
               };
            }
            keyboardInline[i] = keyboardButtons;
         }
         return keyboardInline;
      }

      private static InlineKeyboardButton[][] GetInlineKeyboardPosts(string[] stringArray, string chanel)
      {
         var keyboardInline = new InlineKeyboardButton[stringArray.Length + 1][];
         for (int i = 0; i < stringArray.Length + 1; i++) {
            var keyboardButtons = new InlineKeyboardButton[1];
            if (i + 1 != stringArray.Length + 1) {
               keyboardButtons[0] = new InlineKeyboardButton {
                  Text = stringArray[i].Split('‼')[1],
                  CallbackData = "MorePost♥" + stringArray[i],
               };
            }
            else {
               keyboardButtons[0] = new InlineKeyboardButton {
                  Text = "Назад",
                  CallbackData = "MoreChanel♥" + chanel,
               };
            }
            keyboardInline[i] = keyboardButtons;
         }
         return keyboardInline;
      }

      private static InlineKeyboardButton[][] GetInlineKeyboardLoops(string[] stringArray)
      {
         var keyboardInline = new InlineKeyboardButton[stringArray.Length + 1][];
         for (int i = 0; i < stringArray.Length + 1; i++) {
            var keyboardButtons = new InlineKeyboardButton[1];
            if (i + 1 != stringArray.Length + 1) {
               keyboardButtons[0] = new InlineKeyboardButton {
                  Text = stringArray[i].Split('‼')[0],
                  CallbackData = "MoreLoop♥" + stringArray[i],
               };
            }
            else {
               keyboardButtons[0] = new InlineKeyboardButton {
                  Text = "Добавить цикл",
                  CallbackData = "AddLoop"
               };
            }
            keyboardInline[i] = keyboardButtons;
         }
         return keyboardInline;
      }

      private static IAlbumInputMedia[] GetMedia(string[] stringArray, string text)
      {
         var media = new IAlbumInputMedia[stringArray.Length];
         for (int i = 0; i < stringArray.Length; i++) {
            var mediaItem = new IAlbumInputMedia[1];
            if (i == 0) {
               if (stringArray[i].Split('|')[0] == "Photo") mediaItem[0] = new InputMediaPhoto(stringArray[i].Split('|')[1]) { Caption = text };
               else if (stringArray[i].Split('|')[0] == "Video") mediaItem[0] = new InputMediaVideo(stringArray[i].Split('|')[1]) { Caption = text };
               else if (stringArray[i].Split('|')[0] == "Document") mediaItem[0] = new InputMediaAudio(stringArray[i].Split('|')[1]) { Caption = text };
               else if (stringArray[i].Split('|')[0] == "Audio") mediaItem[0] = new InputMediaDocument(stringArray[i].Split('|')[1]) { Caption = text };
            }
            else {
               if (stringArray[i].Split('|')[0] == "Photo") mediaItem[0] = new InputMediaPhoto(stringArray[i].Split('|')[1]);
               else if (stringArray[i].Split('|')[0] == "Video") mediaItem[0] = new InputMediaVideo(stringArray[i].Split('|')[1]);
               else if (stringArray[i].Split('|')[0] == "Document") mediaItem[0] = new InputMediaAudio(stringArray[i].Split('|')[1]);
               else if (stringArray[i].Split('|')[0] == "Audio") mediaItem[0] = new InputMediaDocument(stringArray[i].Split('|')[1]);
            }
            media[i] = mediaItem[0];
         }
         return media;
      }
   }
}
