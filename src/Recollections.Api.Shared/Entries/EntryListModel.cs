using System;
using System.Collections.Generic;

namespace Neptuo.Recollections.Entries;

public record EntryListModel
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
    int GpsCount
);