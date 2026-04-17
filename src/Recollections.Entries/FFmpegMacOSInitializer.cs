using System;
using System.IO;
using System.Runtime.InteropServices;
using FFMediaToolkit;
using HeyRed.ImageSharp.AVCodecFormats;

namespace Neptuo.Recollections.Entries
{
    // Development-only helper: ImageSharp.AVCodecFormats.Native ships no macOS
    // payload, so on macOS we point FFmpeg at a locally installed FFmpeg 7
    // (e.g. Homebrew `ffmpeg@7`). FFmpeg.AutoGen 7.1.1 is pinned to the
    // FFmpeg 7.x ABI, so ffmpeg 8 will not load.
    internal static class FFmpegMacOSInitializer
    {
        private static readonly string[] CandidatePaths =
        {
            "/opt/homebrew/opt/ffmpeg@7/lib",
            "/usr/local/opt/ffmpeg@7/lib",
        };

        private static bool initialized;

        public static void Initialize()
        {
            if (initialized)
                return;

            initialized = true;

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return;

            if (!string.IsNullOrWhiteSpace(FFmpegBinaries.Path))
                return;

            var envPath = Environment.GetEnvironmentVariable("RECOLLECTIONS_FFMPEG_PATH");
            if (!string.IsNullOrWhiteSpace(envPath) && ContainsFFmpegDylibs(envPath))
            {
                Apply(envPath);
                return;
            }

            foreach (var candidate in CandidatePaths)
            {
                if (ContainsFFmpegDylibs(candidate))
                {
                    Apply(candidate);
                    return;
                }
            }
        }

        private static void Apply(string path)
        {
            // Set both: FFmpegLoader is what FFMediaToolkit actually uses to
            // locate the dylibs, FFmpegBinaries.Path short-circuits the
            // library's auto-finder so it doesn't overwrite us.
            FFmpegLoader.FFmpegPath = path;
            FFmpegBinaries.Path = path;
        }

        private static bool ContainsFFmpegDylibs(string directory)
            => Directory.Exists(directory) && File.Exists(Path.Combine(directory, "libavcodec.61.dylib"));
    }
}
