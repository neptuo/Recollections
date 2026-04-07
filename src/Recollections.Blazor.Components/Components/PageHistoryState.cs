using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neptuo.Recollections.Components
{
    public class PageHistoryState
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public MapPosition Map { get; set; }

        public TimelinePosition Timeline { get; set; }

        public static PageHistoryState Parse(string state)
        {
            if (string.IsNullOrEmpty(state))
                return new PageHistoryState();

            try
            {
                using var document = JsonDocument.Parse(state);
                var root = document.RootElement;
                var result = new PageHistoryState();

                if (root.ValueKind != JsonValueKind.Object)
                    return result;

                if (root.TryGetProperty(nameof(Map), out var mapElement) && mapElement.ValueKind == JsonValueKind.Object)
                    result.Map = mapElement.Deserialize<MapPosition>();
                else if (root.TryGetProperty(nameof(MapPosition.Latitude), out _))
                    result.Map = root.Deserialize<MapPosition>();

                if (root.TryGetProperty(nameof(Timeline), out var timelineElement) && timelineElement.ValueKind == JsonValueKind.Object)
                    result.Timeline = timelineElement.Deserialize<TimelinePosition>();
                else if (root.TryGetProperty(nameof(TimelinePosition.Offset), out _) || root.TryGetProperty(nameof(TimelinePosition.EntryId), out _))
                    result.Timeline = root.Deserialize<TimelinePosition>();

                return result;
            }
            catch (JsonException)
            {
                return new PageHistoryState();
            }
        }

        public string ToJson()
            => JsonSerializer.Serialize(this, SerializerOptions);
    }

    public record TimelinePosition(int Offset, string EntryId);
}
