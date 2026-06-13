using System;

namespace Reproductor_musical.Models
{
    public class SongModel
    {
        public string FilePath { get; set; }
        public string Title { get; set; } = "Sin archivo cargado";
        public TimeSpan CurrentTime { get; set; }
        public TimeSpan TotalTime { get; set; }
        public float Volume { get; set; } = 0.8f;
        public bool IsPlaying { get; set; }
        public bool IsLoaded => !string.IsNullOrEmpty(FilePath);
    }
}
