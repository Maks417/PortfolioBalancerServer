using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PortfolioBalancerServer.Models
{
    public record ExchangeCourse
	{
        public DateTime Date { get; set; }
        public DateTime PreviousDate { get; set; }
        public string PreviousURL { get; set; }
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("Valute")]
        public Dictionary<string, Currency> Currency { get; set; }
    }

    public record Currency
    {
        public string ID { get; set; }
        public string NumCode { get; set; }
        public string CharCode { get; set; }
        public int Nominal { get; set; }
        public string Name { get; set; }
        public decimal Value { get; set; }
        public double Previous { get; set; }
    }
}
