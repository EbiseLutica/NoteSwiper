using System.Collections.Generic;

using Newtonsoft.Json;

public class User
{
    [JsonProperty("host")]
    public string Host { get; set; }

    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("notesCount")]
    public long NotesCount { get; set; }

    [JsonProperty("pinnedNotes")]
    public IEnumerable<Note> PinnedNotes { get; set; }

    [JsonProperty("username")]
    public string Username { get; set; }
}