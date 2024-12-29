namespace DiscordBot.Exceptions;

public class StatusBasedException : Exception
{
    public int StatusCode { get; }

    public StatusBasedException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}