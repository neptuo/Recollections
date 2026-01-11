using System.Collections.Generic;

namespace Neptuo.Recollections.Entries;

public record PageableList<T>(
    List<T> Models,
    bool HasMore
);