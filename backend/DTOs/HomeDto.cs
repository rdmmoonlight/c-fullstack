using System.Text.Json.Serialization;

namespace AumoBackend.DTOs.Home
{
    public class ApiIndonesiaKursResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public KursData? Data { get; set; }
    }

    public class KursData
    {
        [JsonPropertyName("base")]
        public string Base { get; set; } = string.Empty;

        [JsonPropertyName("target")]
        public string Target { get; set; } = string.Empty;

        [JsonPropertyName("rate")]
        public decimal Rate { get; set; }

        [JsonPropertyName("change")]
        public decimal Change { get; set; }
    }

    public class MarketIndicatorDto
    {
        public string Symbol { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        // Mendukung baik input decimal murni maupun string (dikonversi otomatis secara aman)
        private decimal price;

        public object Price
        {
            get => this.price;
            set
            {
                if (value is decimal decValue)
                {
                    this.price = decValue;
                }
                else if (value != null && decimal.TryParse(value.ToString(), out decimal parsedValue))
                {
                    this.price = parsedValue;
                }
                else
                {
                    this.price = 0m;
                }
            }
        }

        private decimal change;

        public object Change
        {
            get => this.change;
            set
            {
                if (value is decimal decValue)
                {
                    this.change = decValue;
                }
                else if (value != null && decimal.TryParse(value.ToString(), out decimal parsedValue))
                {
                    this.change = parsedValue;
                }
                else
                {
                    this.change = 0m;
                }
            }
        }

        // Fleksibel: dapat di-assign manual ATAU mengkalkulasi otomatis dari Change jika tidak di-assign
        private bool? isUp;

        public bool IsUp
        {
            get
            {
                if (this.isUp.HasValue)
                {
                    return this.isUp.Value;
                }

                return this.change >= 0;
            }
            set => this.isUp = value;
        }
    }

    public class KursDataDto
    {
        public string Base { get; set; } = string.Empty;

        public string Target { get; set; } = string.Empty;

        public decimal Rate { get; set; }

        public decimal Change { get; set; }
    }

    public class ApiResponseDto<T>
    {
        public bool Success { get; set; }

        public T? Data { get; set; }

        public string? Error { get; set; }

        public string? Message { get; set; }
    }
}
