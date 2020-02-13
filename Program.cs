using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NoteSwiper
{
    class Program
    {
        static string endpoint;
        static async Task Main(string[] args)
        {
            string token;
            string host;

            // -- 認証
            Console.WriteLine("Write your token. It's at Settings > API.");
            do
            {
                Console.Write("> ");
                token = Console.ReadLine();
            } while (string.IsNullOrWhiteSpace(token));

            Console.WriteLine("Write your server's host. e.g: misskey.io");
            do
            {
                Console.Write("> ");
                host = Console.ReadLine();
            } while (string.IsNullOrWhiteSpace(host));

            endpoint = $"https://{host}/api/";

            // -- ユーザー情報をフェッチ

            var me = await PostAsync<User>("i", new { i = token });

            Console.WriteLine("Fetched your account:");
            Console.WriteLine($" {me.Name ?? me.Username} @{me.Name}");
            Console.WriteLine($" {me.NotesCount} Notes");
            Console.WriteLine($" id: {me.Id}");

            // ピン留めの解除

            var pinnedCount = 0;

            foreach (var note in me.PinnedNotes)
            {
                await PostAsync("i/unpin", new
                {
                    noteId = note.Id,
                    i = token
                });
                Console.WriteLine($"Unpinned note: {note}");
                pinnedCount++;
            }

            Console.WriteLine($"Unpinned {pinnedCount} notes");

            // -- 全ノートのフェッチ

            var notes = new List<Note>();

            var needsFetchingAllNotes = true;

            if (File.Exists("notes.json"))
            {
                string yesno;
                Console.WriteLine("You have `notes.json`. By using this file, no longer need to make API requests for all notes. May I import this file?");
                do
                {
                    Console.Write(" (Y,n) > ");
                    yesno = Console.ReadLine().ToLowerInvariant();
                } while (yesno != "y" && yesno != "n");

                if (yesno == "y")
                {
                    needsFetchingAllNotes = false;
                    var n = JsonConvert.DeserializeObject<List<Note>>(File.ReadAllText("notes.json"));
                    foreach (var note in n)
                    {
                        notes.Add(note);
                        Console.WriteLine($"Imported: {note}");
                    }
                }
            }

            if (needsFetchingAllNotes)
            {
                Console.WriteLine("Fetching your all notes. It takes some time...");

                string untilId = null;

                while (true)
                {
                    var fetched = await PostAsync<List<Note>>("users/notes",
                    untilId == null ? (object)new
                    {
                        userId = me.Id,
                        limit = 100,
                        i = token,
                    } : new
                    {
                        userId = me.Id,
                        untilId,
                        limit = 100,
                        i = token,
                    }
            ); ;
                    if (!fetched.Any())
                        break;
                    untilId = fetched.Last().Id;
                    notes.AddRange(fetched);
                    Console.WriteLine($"Fetched {notes.Count}/{me.NotesCount} notes.");
                }
            }
            Console.WriteLine($"Fetched your {notes.Count} notes!");

            // createdAt で並び替えを行う
            notes = notes.OrderByDescending(n => n.CreatedAt).ToList();

            for (var i = 0; i < notes.Count; i++)
            {
                var note = notes[i];
                try
                {
                    await PostAsync("notes/delete", new
                    {
                        noteId = note.Id,
                        i = token,
                    });
                    Console.WriteLine($"Deleted notes {i}/{notes.Count}");
                }
                catch (ApiErrorException e)
                {
                    Console.WriteLine($"Exception thrown when deleting notes {i}: {e.Message}");
                    Console.WriteLine("Retry after 15 minutes");
                    await Task.Delay(899000);
                    i--;
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"Exception thrown when deleting notes {i}: {e.Message}");
                    Console.WriteLine("Retry after 15 minutes");
                    await Task.Delay(899000);
                    i--;
                }
                await Task.Delay(1000);
            }

            Console.WriteLine("Press ENTER to exit");
            Console.ReadLine();
        }

        static async Task<List<Note>> GetUsersNotesAsync(string userId, string untilId)
        {
            return await PostAsync<List<Note>>("users/notes",
                untilId == null ? (object)new
                {
                    userId,
                    limit = 100,
                } : new
                {
                    userId,
                    untilId,
                    limit = 100,
                }
            );
        }

        static async Task PostAsync(string api, object args)
        {
            await PostAsync<object>(api, args);
        }

        static async Task<T> PostAsync<T>(string api, object args)
        {
            var res = await cli.PostAsync(endpoint + api, new StringContent(JsonConvert.SerializeObject(args)));
            var jsonString = await res.Content.ReadAsStringAsync();
            try
            {
                // エラーオブジェクトであれば例外発生
                var err = JsonConvert.DeserializeObject<Error>(jsonString);
                if (err != null && new[] { err.Code, err.Message }.All(el => el != null))
                {
                    throw new ApiErrorException(err);
                }
            }
            catch (JsonSerializationException)
            {
                // JSON解析エラーが出るということはエラーオブジェクトではないので無視
            }
            if ((int)res.StatusCode >= 400)
            {
                throw new HttpRequestException(jsonString);
            }
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        private static HttpClient cli = new HttpClient();
    }

    [System.Serializable]
    public class ApiErrorException : System.Exception
    {
        public ApiErrorException(Error error) : base(error.Message) { }
    }

    public class Error
    {
        [JsonProperty("message")]
        public string Message { get; set; }
        [JsonProperty("code")]
        public string Code { get; set; }
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("kind")]
        public string Kind { get; set; }
    }
}
