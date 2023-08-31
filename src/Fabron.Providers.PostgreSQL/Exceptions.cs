namespace Fabron.Providers.PostgreSQL;

public class FabronPostgreSQLProviderException : Exception
{
    public FabronPostgreSQLProviderException(string? message) : base(message)
    {
    }

    public FabronPostgreSQLProviderException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
