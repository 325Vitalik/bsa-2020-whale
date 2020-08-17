﻿using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Whale.Shared.DTO.Meeting;
using Whale.Shared.DTO.Meeting.MeetingMessage;
using Whale.BLL.Interfaces;

namespace Whale.BLL.Hubs
{
    public class MeetingHub : Hub
    {
        private readonly IMeetingService _meetingService;

        public MeetingHub(IMeetingService meetingService)
        {
            _meetingService = meetingService;
        }

        [HubMethodName("OnUserConnect")]
        public async Task Join(MeetingConnectDTO connectionData)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, connectionData.MeetingId);
            await Clients.Group(connectionData.MeetingId).SendAsync("OnUserConnect", connectionData);
        }

        [HubMethodName("OnUserDisconnect")]
        public async Task Disconnect(MeetingConnectDTO ConnectionData)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, ConnectionData.MeetingId);
            await Clients.Group(ConnectionData.MeetingId).SendAsync("OnUserDisconnect", ConnectionData);
        }

        [HubMethodName("OnSendMessage")]
        public async Task SendMessage(MeetingMessageCreateDTO msgDTO)
        {
            var msg = await _meetingService.SendMessage(msgDTO);
            await Clients.Group(msgDTO.MeetingId).SendAsync("OnSendMessage", msg);
        }

        [HubMethodName("OnGetMessages")]
        public async Task GetMessages(string groupName)
        {
            var messages = _meetingService.GetMessages(groupName);
            await Clients.Caller.SendAsync("OnGetMessages", messages);
        }

        [HubMethodName("OnConferenceStartRecording")]
        public async Task OnConferenceStartRecording(string message)
        {
            await Clients.All.SendAsync("OnConferenceStartRecording", message);
        }

        [HubMethodName("OnConferenceStopRecording")]
        public async Task OnConferenceStopRecording(string message)
        {
            await Clients.All.SendAsync("OnConferenceStopRecording", message);
        }

        [HubMethodName("OnDrawing")]
        public async Task Draw(CreateDrawingDTO drawingDTO)
        {
            await Clients.GroupExcept(drawingDTO.MeetingId, new List<string> { Context.ConnectionId }).SendAsync("OnDrawing", drawingDTO.CanvasEvent);
        }

        [HubMethodName("OnErasing")]
        public async Task Erase(EraseDrawingDTO drawingDTO)
        {
            await Clients.GroupExcept(drawingDTO.MeetingId, new List<string> { Context.ConnectionId }).SendAsync("OnErasing", drawingDTO.Erase);
        }
    }
}
