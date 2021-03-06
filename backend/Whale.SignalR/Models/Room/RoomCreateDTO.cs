﻿using System.Collections.Generic;

namespace Whale.SignalR.Models.Room
{
    public class RoomCreateDTO
    {
        public string MeetingId { get; set; }
        public string MeetingLink { get; set; }
        public string RoomName { get; set; }
        public double Duration { get; set; } = 10;
        public ICollection<string> ParticipantsIds { get; set; }
    }
}
