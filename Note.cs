using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

public class Note
{
    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("cw")]
    public string Cw { get; set; }

    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("mediaIds")]
    public List<string> MediaIds { get; set; }

    [JsonProperty("mentions")]
    public List<string> Mentions { get; set; }

    [JsonProperty("reply")]
    public Note Reply { get; set; }

    [JsonProperty("replyId")]
    public string ReplyId { get; set; }

    [JsonProperty("renote")]
    public Note Renote { get; set; }

    [JsonProperty("renoteId")]
    public string RenoteId { get; set; }

    [JsonProperty("text")]
    public string Text { get; set; }

    [JsonProperty("userId")]
    public string UserId { get; set; }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append(Cw != null ? Cw : Text ?? "(No text)");
        if (Renote != null)
        {
            builder.Append(" RN: ").Append(Renote);
        }
        if (Reply != null)
        {
            builder.Append(" RE: ").Append(Reply);
        }

        if (MediaIds != null && MediaIds.Count > 0)
        {
            builder.Append($" ({MediaIds.Count} medias)");
        }
        return builder.ToString();
    }
}
