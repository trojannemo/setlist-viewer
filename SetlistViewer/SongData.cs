namespace SetlistViewer
{
    using System.Text.Json.Serialization;

    public class SongData
    {
        [JsonPropertyName("artist")]
        public string Artist { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("vocal_parts")]
        public int VocalParts { get; set; }

        [JsonPropertyName("duration")]
        public string Duration { get; set; }

        [JsonPropertyName("drums_diff")]
        public string DrumsDiff { get; set; }

        [JsonPropertyName("bass_diff")]
        public string BassDiff { get; set; }

        [JsonPropertyName("guitar_diff")]
        public string GuitarDiff { get; set; }

        [JsonPropertyName("keys_diff")]
        public string KeysDiff { get; set; }

        [JsonPropertyName("vocal_diff")]
        public string VocalDiff { get; set; }

        [JsonPropertyName("rating")]
        public string Rating { get; set; }

        [JsonPropertyName("genre")]
        public string Genre { get; set; }

        [JsonPropertyName("album")]
        public string Album { get; set; }

        [JsonPropertyName("track_number")]
        public int TrackNumber { get; set; }

        [JsonPropertyName("master")]
        public bool Master { get; set; }

        [JsonPropertyName("year_recorded")]
        public int YearRecorded { get; set; }

        [JsonPropertyName("year_released")]
        public int YearReleased { get; set; }

        [JsonPropertyName("subgenre")]
        public string Subgenre { get; set; }

        [JsonPropertyName("proguitar_diff")]
        public string ProGuitarDiff { get; set; }

        [JsonPropertyName("probass_diff")]
        public string ProBassDiff { get; set; }

        [JsonPropertyName("prokeys_diff")]
        public string ProKeysDiff { get; set; }

        [JsonPropertyName("band_diff")]
        public string BandDiff { get; set; }

        [JsonPropertyName("shortname")]
        public string ShortName { get; set; }

        [JsonPropertyName("songid")]
        public long SongId { get; set; }

        [JsonPropertyName("songid_string")]
        public string SongIdString { get; set; }

        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonPropertyName("filepath")]
        public string FilePath { get; set; }

        [JsonPropertyName("midifile")]
        public string MidiFile { get; set; }

        [JsonPropertyName("preview_start")]
        public int PreviewStart { get; set; }

        [JsonPropertyName("preview_end")]
        public int PreviewEnd { get; set; }

        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("scroll_speed")]
        public int ScrollSpeed { get; set; }

        [JsonPropertyName("tonic_note")]
        public int TonicNote { get; set; }

        [JsonPropertyName("tonality")]
        public int Tonality { get; set; }

        [JsonPropertyName("percussion_bank")]
        public string PercussionBank { get; set; }

        [JsonPropertyName("drum_bank")]
        public string DrumBank { get; set; }

        [JsonPropertyName("bass_tuning")]
        public string BassTuning { get; set; }

        [JsonPropertyName("guitar_tuning")]
        public string GuitarTuning { get; set; }

        public int SongIndex { get; set; }
    }


}
