﻿using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Whale.API.Services;
using Whale.Shared.Models.Meeting;

namespace Whale.API.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class MeetingHistoryController : ControllerBase
	{
		private readonly MeetingHistoryService _meetingHistoryService;

		public MeetingHistoryController(MeetingHistoryService meetingHistoryService)
		{
			_meetingHistoryService = meetingHistoryService;
		}

		[HttpGet]
		public async Task<ActionResult<IEnumerable<MeetingDTO>>> GetMeetings(Guid userId, int skip, int take)
		{
			var meetings = await _meetingHistoryService.GetMeetingsWithParticipantsAndPollResults(userId, skip, take);
			return Ok(meetings);
		}

		[HttpGet("script/{id}")]
		public async Task<ActionResult<IEnumerable<MeetingSpeechDTO>>> GetMeetings(Guid id)
		{
			var meetings = await _meetingHistoryService.GetMeetingScript(id);
			return Ok(meetings);
		}
	}
}
