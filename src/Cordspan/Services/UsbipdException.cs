namespace Cordspan.Services;

public sealed class UsbipdException : Exception
{
    public UsbipdException(string message) : base(message)
    {
    }
}
