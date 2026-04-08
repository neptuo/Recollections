using Neptuo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Neptuo.Recollections.Entries
{
    internal static class Mp4StreamingContentTypeProvider
    {
        public static string NormalizeContainerContentType(string contentType, string fileName)
        {
            if (!string.IsNullOrWhiteSpace(contentType))
                return contentType.Trim();

            return Path.GetExtension(fileName)?.ToLowerInvariant() switch
            {
                ".mp4" or ".m4v" => "video/mp4",
                ".mov" => "video/quicktime",
                ".webm" => "video/webm",
                ".ogv" => "video/ogg",
                _ => "video/mp4"
            };
        }

        public static bool HasExplicitCodecs(string contentType)
            => !string.IsNullOrWhiteSpace(contentType) && contentType.Contains("codecs=", StringComparison.OrdinalIgnoreCase);

        public static string NormalizeStreamingContentType(string contentType, string fileName, Stream content)
        {
            string normalizedContentType = NormalizeContainerContentType(contentType, fileName);
            if (string.IsNullOrWhiteSpace(normalizedContentType) || HasExplicitCodecs(normalizedContentType))
                return normalizedContentType;

            string containerType = normalizedContentType.Split(';')[0].Trim();
            if (!StringComparer.OrdinalIgnoreCase.Equals(containerType, "video/mp4") || content == null)
                return normalizedContentType;

            if (!TryReadCodecIdentifiers(content, out List<string> codecs) || codecs.Count == 0)
                return normalizedContentType;

            return $"{containerType}; codecs=\"{string.Join(", ", codecs)}\"";
        }

        private static bool TryReadCodecIdentifiers(Stream content, out List<string> codecs)
        {
            List<string> result = [];
            if (!content.CanSeek)
            {
                codecs = result;
                return false;
            }

            long originalPosition = content.Position;
            try
            {
                content.Position = 0;
                if (!TryReadBoxes(content, content.Length, box =>
                {
                    if (box.Type != "moov")
                        return false;

                    result.AddRange(ReadMovieCodecs(content, box.EndPosition));
                    return result.Count > 0;
                }))
                {
                    codecs = result;
                    return false;
                }

                codecs = result;
                return result.Count > 0;
            }
            finally
            {
                content.Position = originalPosition;
            }
        }

        private static List<string> ReadMovieCodecs(Stream content, long endPosition)
        {
            List<string> codecs = [];
            TryReadBoxes(content, endPosition, box =>
            {
                if (box.Type != "trak")
                    return false;

                if (TryReadTrackCodec(content, box.EndPosition, out string codec))
                    codecs.Add(codec);

                return false;
            });

            return codecs;
        }

        private static bool TryReadTrackCodec(Stream content, long endPosition, out string codec)
        {
            string handlerType = null;
            string codecIdentifier = null;

            TryReadBoxes(content, endPosition, box =>
            {
                if (box.Type != "mdia")
                    return false;

                TryReadMediaCodec(content, box.EndPosition, out handlerType, out codecIdentifier);
                return handlerType != null && codecIdentifier != null;
            });

            codec = handlerType switch
            {
                "vide" or "soun" => codecIdentifier,
                _ => null
            };

            return codec != null;
        }

        private static void TryReadMediaCodec(Stream content, long endPosition, out string handlerType, out string codec)
        {
            string localHandlerType = null;
            string localCodec = null;

            TryReadBoxes(content, endPosition, box =>
            {
                switch (box.Type)
                {
                    case "hdlr":
                        localHandlerType = ReadHandlerType(content, box.EndPosition);
                        break;
                    case "minf":
                        localCodec = ReadMediaInformationCodec(content, box.EndPosition);
                        break;
                }

                return localHandlerType != null && localCodec != null;
            });

            handlerType = localHandlerType;
            codec = localCodec;
        }

        private static string ReadMediaInformationCodec(Stream content, long endPosition)
        {
            string codec = null;
            TryReadBoxes(content, endPosition, box =>
            {
                if (box.Type != "stbl")
                    return false;

                codec = ReadSampleTableCodec(content, box.EndPosition);
                return codec != null;
            });

            return codec;
        }

        private static string ReadSampleTableCodec(Stream content, long endPosition)
        {
            string codec = null;
            TryReadBoxes(content, endPosition, box =>
            {
                if (box.Type != "stsd")
                    return false;

                codec = ReadSampleDescriptionCodec(content, box.EndPosition);
                return codec != null;
            });

            return codec;
        }

        private static string ReadSampleDescriptionCodec(Stream content, long endPosition)
        {
            if (!TrySkip(content, 4))
                return null;

            if (!TryReadUInt32(content, out uint entryCount))
                return null;

            for (uint i = 0; i < entryCount && content.Position < endPosition; i++)
            {
                if (!TryReadBoxHeader(content, endPosition, out BoxHeader sampleEntry))
                    break;

                string codec = sampleEntry.Type switch
                {
                    "avc1" or "avc3" => ReadAvcCodec(content, sampleEntry),
                    "mp4a" => ReadMp4AudioCodec(content, sampleEntry),
                    _ => null
                };

                if (codec != null)
                    return codec;

                content.Position = sampleEntry.EndPosition;
            }

            return null;
        }

        private static string ReadAvcCodec(Stream content, BoxHeader sampleEntry)
        {
            if (!TrySkip(content, 78))
                return null;

            string codec = null;
            TryReadBoxes(content, sampleEntry.EndPosition, box =>
            {
                if (box.Type != "avcC")
                    return false;

                codec = ReadAvcConfigurationCodec(content, sampleEntry.Type, box.EndPosition);
                return codec != null;
            });

            return codec;
        }

        private static string ReadAvcConfigurationCodec(Stream content, string sampleEntryType, long endPosition)
        {
            if (!TrySkip(content, 1))
                return null;

            if (!TryReadByte(content, out byte profile)
                || !TryReadByte(content, out byte compatibility)
                || !TryReadByte(content, out byte level))
            {
                return null;
            }

            content.Position = endPosition;
            return $"{sampleEntryType}.{profile:x2}{compatibility:x2}{level:x2}";
        }

        private static string ReadMp4AudioCodec(Stream content, BoxHeader sampleEntry)
        {
            if (!TrySkip(content, 28))
                return null;

            string codec = null;
            TryReadBoxes(content, sampleEntry.EndPosition, box =>
            {
                if (box.Type != "esds")
                    return false;

                codec = ReadEsdsCodec(content, box.EndPosition);
                return codec != null;
            });

            return codec;
        }

        private static string ReadEsdsCodec(Stream content, long endPosition)
        {
            if (!TrySkip(content, 4))
                return null;

            if (!TryReadDescriptor(content, endPosition, out byte tag, out long descriptorEnd) || tag != 0x03)
                return null;

            if (!TrySkip(content, 3))
                return null;

            if (!TryReadDescriptor(content, descriptorEnd, out tag, out long decoderConfigEnd) || tag != 0x04)
                return null;

            if (!TryReadByte(content, out byte objectTypeIndication))
                return null;

            if (!TrySkip(content, 12))
                return null;

            while (content.Position < decoderConfigEnd)
            {
                if (!TryReadDescriptor(content, decoderConfigEnd, out tag, out long nestedEnd))
                    return null;

                if (tag == 0x05)
                {
                    int length = checked((int)(nestedEnd - content.Position));
                    if (length <= 0)
                        return null;

                    byte[] decoderSpecificInfo = new byte[length];
                    if (content.Read(decoderSpecificInfo, 0, length) != length)
                        return null;

                    if (!TryReadAudioObjectType(decoderSpecificInfo, out int audioObjectType))
                        return null;

                    return $"mp4a.{objectTypeIndication:x2}.{audioObjectType}";
                }

                content.Position = nestedEnd;
            }

            return null;
        }

        private static bool TryReadAudioObjectType(byte[] decoderSpecificInfo, out int audioObjectType)
        {
            audioObjectType = 0;
            if (decoderSpecificInfo.Length == 0)
                return false;

            audioObjectType = decoderSpecificInfo[0] >> 3;
            if (audioObjectType != 31)
                return audioObjectType > 0;

            if (decoderSpecificInfo.Length < 2)
                return false;

            audioObjectType = 32 + (((decoderSpecificInfo[0] & 0x07) << 3) | (decoderSpecificInfo[1] >> 5));
            return audioObjectType > 0;
        }

        private static string ReadHandlerType(Stream content, long endPosition)
        {
            if (!TrySkip(content, 8))
                return null;

            if (!TryReadString(content, 4, out string handlerType))
                return null;

            content.Position = endPosition;
            return handlerType;
        }

        private static bool TryReadBoxes(Stream content, long endPosition, Func<BoxHeader, bool> reader)
        {
            while (content.Position < endPosition)
            {
                if (!TryReadBoxHeader(content, endPosition, out BoxHeader box))
                    return false;

                bool shouldStop = reader(box);
                content.Position = box.EndPosition;
                if (shouldStop)
                    return true;
            }

            return true;
        }

        private static bool TryReadDescriptor(Stream content, long endPosition, out byte tag, out long descriptorEnd)
        {
            tag = 0;
            descriptorEnd = content.Position;
            if (!TryReadByte(content, out tag))
                return false;

            int length = 0;
            for (int i = 0; i < 4; i++)
            {
                if (!TryReadByte(content, out byte next))
                    return false;

                length = (length << 7) | (next & 0x7F);
                if ((next & 0x80) == 0)
                {
                    descriptorEnd = content.Position + length;
                    return descriptorEnd <= endPosition;
                }
            }

            return false;
        }

        private static bool TryReadBoxHeader(Stream content, long endPosition, out BoxHeader box)
        {
            box = default;
            if (content.Position + 8 > endPosition)
                return false;

            long boxStart = content.Position;
            if (!TryReadUInt32(content, out uint size) || !TryReadString(content, 4, out string type))
                return false;

            long headerSize = 8;
            long boxSize = size;
            if (size == 1)
            {
                if (!TryReadUInt64(content, out ulong largeSize))
                    return false;

                boxSize = (long)largeSize;
                headerSize = 16;
            }
            else if (size == 0)
            {
                boxSize = endPosition - boxStart;
            }

            if (boxSize < headerSize)
                return false;

            long boxEnd = boxStart + boxSize;
            if (boxEnd > endPosition)
                return false;

            box = new BoxHeader(type, boxEnd);
            return true;
        }

        private static bool TryReadUInt32(Stream content, out uint value)
        {
            value = 0;
            byte[] buffer = new byte[4];
            if (content.Read(buffer, 0, buffer.Length) != buffer.Length)
                return false;

            value = ((uint)buffer[0] << 24)
                | ((uint)buffer[1] << 16)
                | ((uint)buffer[2] << 8)
                | buffer[3];
            return true;
        }

        private static bool TryReadUInt64(Stream content, out ulong value)
        {
            value = 0;
            byte[] buffer = new byte[8];
            if (content.Read(buffer, 0, buffer.Length) != buffer.Length)
                return false;

            value = ((ulong)buffer[0] << 56)
                | ((ulong)buffer[1] << 48)
                | ((ulong)buffer[2] << 40)
                | ((ulong)buffer[3] << 32)
                | ((ulong)buffer[4] << 24)
                | ((ulong)buffer[5] << 16)
                | ((ulong)buffer[6] << 8)
                | buffer[7];
            return true;
        }

        private static bool TryReadByte(Stream content, out byte value)
        {
            int read = content.ReadByte();
            if (read < 0)
            {
                value = 0;
                return false;
            }

            value = (byte)read;
            return true;
        }

        private static bool TryReadString(Stream content, int length, out string value)
        {
            value = null;
            byte[] buffer = new byte[length];
            if (content.Read(buffer, 0, buffer.Length) != buffer.Length)
                return false;

            value = Encoding.ASCII.GetString(buffer);
            return true;
        }

        private static bool TrySkip(Stream content, int length)
        {
            Ensure.PositiveOrZero(length, "length");
            if (!content.CanSeek || content.Position + length > content.Length)
                return false;

            content.Position += length;
            return true;
        }

        private readonly struct BoxHeader(string type, long endPosition)
        {
            public string Type { get; } = type;
            public long EndPosition { get; } = endPosition;
        }
    }
}

