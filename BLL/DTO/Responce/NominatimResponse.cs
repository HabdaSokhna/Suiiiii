using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace BLL.DTO.Responce
{
    public class NominatimResponse
    {
        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; }
    }
}
