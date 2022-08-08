﻿using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Onwrd.EntityFrameworkCore.Internal.EventExtraction
{
    internal static class EntityEntryExtensions
    {
        internal static List<object> ExtractEvents(this EntityEntry entityEntry)
        {
            var events = new List<object>();

            if (entityEntry.Entity is EventRaiser eventRaiser)
            {
                events.AddRange(eventRaiser.Events);
                eventRaiser.ClearEvents();
            }

            return events;
        }
    }
}
