using System;
using System.Collections.Generic;

namespace Neptuo.Recollections.Entries;

public record HighestAltitudeEntryListModel
(
    string UserId,
    string UserName,

    string Id,
    string Title,
    int TextWordCount,
    DateTime When,

    string StoryTitle,
    string ChapterTitle,

    List<EntryBeingModel> Beings,

    int ImageCount,
    int VideoCount,
    int GpsCount,
    double Altitude
)
    : EntryListModel(UserId, UserName, Id, Title, TextWordCount, When, StoryTitle, ChapterTitle, Beings, ImageCount, VideoCount, GpsCount);
