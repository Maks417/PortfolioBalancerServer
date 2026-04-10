namespace PortfolioBalancerServer.Services;

public class ExchangeRatesUnavailableException : Exception
{
    public ExchangeRatesUnavailableException()
        : base("Exchange rates are unavailable.")
    {
    }

    public ExchangeRatesUnavailableException(string message)
        : base(message)
    {
    }

    public ExchangeRatesUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
