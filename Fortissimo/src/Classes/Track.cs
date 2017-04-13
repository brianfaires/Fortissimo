using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fortissimo
{
    public class Track
    {
        public class Event
        {
            public long absTickCount;
            public long absTimeMS;
            public int eventType;
            public int channel;
            public int param1;
            public int param2;

            public Event(long absTickCount, long absTimeMS, int eventType, int channel, int param1, int param2)
            {
                this.absTickCount = absTickCount;
                this.absTimeMS = absTimeMS;
                this.eventType = eventType;
                this.channel = channel;
                this.param1 = param1;
                this.param2 = param2;
            }
        }

        public String instrument;
        public String name;

        public String trackID;
        public int size;
        public List<Event> allEvents;

        public Track()
        {
            allEvents = new List<Event>(300); // Arbitrary starting value for list size
        }

        public bool AddEvent(long absTickCount, long absTimeMS, int eventType, int channel, int param1, int param2)
        {
            try
            {
                allEvents.Add(new Event(absTickCount, absTimeMS, eventType, channel, param1, param2));
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
        public bool AddMetaEvent(long absTickCount, long absTimeMS, int command, int param1, int param2, int param3)
        {
            try
            {
                allEvents.Add(new Event(absTickCount, absTimeMS, MetaCommandToEventType(command), param1, param2, param3));
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
        public static int MetaCommandToEventType(int metaCommand)
        {
            return metaCommand + 1000;
        }
        public static int EventTypeToMetaCommand(int eventType)
        {
            return eventType - 1000;
        }
    }
}
