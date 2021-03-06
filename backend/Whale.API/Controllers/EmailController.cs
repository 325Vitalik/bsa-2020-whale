﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Whale.API.Services;
using Whale.DAL.Models.Email;
using Whale.Shared.Models.Email;

namespace Whale.API.Controllers
{
	[ApiController]
	[Route("email")]
	public class EmailController: ControllerBase
	{
		private readonly EmailService _emailServcice;

		public EmailController(EmailService emailService)
		{
			_emailServcice = emailService;
		}

		[HttpPost]
		public async Task<OkResult> SendEmail([FromBody] MeetingInviteDTO inviteDto)
		{
			await _emailServcice.SendMeetingInvitesAsync(inviteDto);
			return Ok();
		}

		[HttpPost("scheduled")]
		public async Task<OkResult> SendScheduledEmail([FromBody] ScheduledMeetingInvite invite)
		{
			await _emailServcice.SendMeetingInviteToHostAsync(invite);
			return Ok();
		}
	}
}
