﻿using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Whale.BLL.Interfaces;
using Whale.DAL;
using Whale.Shared.DTO.Call;

namespace Whale.BLL.Hubs
{
    public sealed class ChatHub : Hub
    {
        private readonly IMeetingService _meetingService;

        public ChatHub(IMeetingService meetingService)
        {
            _meetingService = meetingService;
        }

        [HubMethodName("JoinGroup")]
        public async Task Join(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            await Clients.Group(groupName).SendAsync("JoinedGroup", Context.ConnectionId);
        }

        public async Task Disconnect(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            await Clients.Group(groupName).SendAsync(Context.ConnectionId + " jeft groupS");
        }

        [HubMethodName("OnStartCall")]
        public async Task StartCall(StartCallDTO startCallDTO)
        {
            var link = await _meetingService.CreateMeeting(startCallDTO.Meeting, startCallDTO.Emails);
            await Clients.Group(startCallDTO.ContactId).SendAsync("OnStartCall", link);
        }

        [HubMethodName("OnTakeCall")]
        public async Task TakeCall(string groupName)
        {
            await Clients.OthersInGroup(groupName).SendAsync("OnTakeCall");
        }

        [HubMethodName("OnDeclineCall")]
        public async Task DeclineCall(DeclineCallDTO declineCallDTO)
        {
            await _meetingService.ParticipantDisconnect(declineCallDTO.ContactId, declineCallDTO.Email);
            await Clients.OthersInGroup(declineCallDTO.ContactId).SendAsync("OnDeclineCall");
        }
    }
}