namespace Archiver;

public class DecodingException : Exception
{
    public DecodingException() {}

    public DecodingException(string msg) : base(msg) {}
}