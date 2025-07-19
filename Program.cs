using System;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace BotsDock
{
    internal class Program
    {
        private static NotifyIcon _notifyIcon;
        static void Main(string[] args)
        {
            _notifyIcon = new NotifyIcon { 
            Icon = SystemIcons.Hand,
            Text = "Alchemy Utils",
            Visible = true
            };

            if (!IsInStartup())
                AddToStartup();
            var contexMenu = new ContextMenuStrip();
            //contexMenu.Items.Add("Open", null, (s, e) => ShowConsole());
            contexMenu.Items.Add("Exit", null, (s, e) => ExitApp());
            _notifyIcon.ContextMenuStrip = contexMenu;

            _notifyIcon.DoubleClick += (s, e) => ShowConsole();

            //Console.Title = "Alchemy Utils";
            //HideConsole(); // throw error

            string token = File.ReadAllText("token.txt");
            var client = new TelegramBotClient(token);
            client.StartReceiving(Update, Error);
            Console.WriteLine($"[Info] Server is running");
            Console.ReadLine();
            Application.Run();
        }

        //static void HideConsole() => ShowWindow(GetConsoleWindow(), SW_HIDE);
        static void HideConsole()
        {
            var handle = NativeMethods.GetConsoleWindow();
            NativeMethods.ShowWindow(handle, NativeMethods.SW_HIDE);
        }
        //static void ShowConsole() => ShowWindow(GetConsoleWindow(), SW_SHOW);
        static void ShowConsole()
        {
            var handle = NativeMethods.GetConsoleWindow();
            NativeMethods.ShowWindow(handle, NativeMethods.SW_SHOW);
        }
        static void ExitApp()
        {
            _notifyIcon.Visible = false;
            _notifyIcon?.Dispose();
            Application.Exit();
            Environment.Exit(0);
        }

        static void AddToStartup()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                    "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string? exePath = GetExePath();
                if (exePath == null || exePath == "")
                    exePath = "empty_path";
                if (!exePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    throw new Exception("Указанный файл не является исполняемым (.exe)");

                key.SetValue("AlchemyUtils", $"\"{exePath}\"");

                //Console.WriteLine("[Info] App has been added to autoloading");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error]: {ex.Message}");
            }
        }
        static string? GetExePath()
        {
            if (Environment.ProcessPath != null)
                return Environment.ProcessPath;

            return Process.GetCurrentProcess().MainModule.FileName;
        }

        static bool IsInStartup()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
            return key?.GetValue("AlchemyUtils") != null;
        }

        static void RemoveFromStartup()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            key?.DeleteValue("AlchemyUtils", false);
        }


        static async Task Update(ITelegramBotClient botClient, Update update, CancellationToken token)
        {
            var message = update.Message;
            if (message == null)
            {
                Console.WriteLine($"Null message");
                return;
            }
            if (message.Text != null)
            {
                Console.WriteLine($"[User] {message.Chat.Username ?? "Unknown"}:   {message.Text}");
                //place for command hadler method call
                if (message.Text.ToLower().Contains("/check"))
                {
                    await botClient.SendMessage(message.Chat.Id, "Bot is running");
                }
            }
            if (message.Photo != null)
            {
                await botClient.SendMessage(message.Chat.Id, "Okay, send me as a file");
                return;
            }
            if (message.Document != null)
            {
                await botClient.SendMessage(message.Chat.Id, "File is accepted. Sending to server...");

                var fileId = message.Document.FileId;
                var tgFile = await botClient.GetFile(fileId);
                var filePath = tgFile.FilePath;

                //download file from user
                string destinationFilePath = $@"files\downloaded.png";
                Directory.CreateDirectory("files");
                await using FileStream fileStream = File.Create("files/downloaded.png");
                if (fileStream == null)
                {
                    await botClient.SendMessage(message.Chat.Id, "[Error] Can't open file");
                    Console.WriteLine($"[Error] Can't open user file");
                    return;
                }
                await botClient.DownloadFile(tgFile, fileStream);
                //Console.WriteLine($"File path: {destinationFilePath}");
                try
                {
                    if (File.Exists(destinationFilePath))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = destinationFilePath,
                            UseShellExecute = true
                        });
                    }
                    else
                    {
                        //Console.WriteLine("File does not exist!");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                fileStream.Close();
                await botClient.SendMessage(message.Chat.Id, "File succesfully uploaded");

                //upload file
                //await using var stream = File.OpenRead(destinationFilePath);
                ////message.Document.FileName = message.Document.FileName.Replace(".png", "(edited).png");
                //var sendingMsg = await botClient.SendDocument(message.Chat.Id, stream);
                //stream.Close();

                return;
            }
        }

        private static async Task Error(ITelegramBotClient client, Exception exception, HandleErrorSource source, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }

    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;
    }
}
