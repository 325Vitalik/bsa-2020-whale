﻿using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Whale.BLL.Exceptions;
using Whale.BLL.Hubs;
using Whale.BLL.Services.Abstract;
using Whale.DAL;
using Whale.DAL.Models;
using Whale.DAL.Models.Poll;
using Whale.Shared.DTO.Poll;
using Whale.Shared.Helpers;
using Whale.Shared.Services;

namespace Whale.BLL.Services
{
	public class PollService : BaseService
	{
		private readonly IHubContext<MeetingHub> _meetingHub;
		private readonly RedisService _redisService;

		public PollService(WhaleDbContext context, IMapper mapper, IHubContext<MeetingHub> meetingHub, RedisService redisService) 
			: base(context, mapper) 
		{
			_meetingHub = meetingHub;
			this._redisService = redisService;
		}

		public async Task<PollDTO> CreatePoll(PollCreateDTO pollDto)
		{
			if(!_context.Meetings.Any(meeting => meeting.Id == pollDto.MeetingId))
			{
				throw new NotFoundException(nameof(Meeting));
			}

			Poll pollEntity = _mapper.Map<Poll>(pollDto);
			pollEntity.Id = Guid.NewGuid();

			_redisService.Connect();
			// create set of Poll Results with poll Id key
			_redisService.Set<Poll>(pollEntity.Id.ToString(), pollEntity);

			var pollDto2 = _mapper.Map<PollDTO>(pollEntity);
			return pollDto2;
		}

		public async Task SavePollAnswer(PollAnswerDTO pollAnswerDto)
		{
			pollAnswerDto.UserId = Guid.NewGuid().ToString();
			_redisService.Connect();
			await _redisService.AddToSet<PollAnswerDTO>(pollAnswerDto.PollId.ToString() + nameof(PollAnswerDTO), pollAnswerDto);

			var pollAnswerDtos = await _redisService.GetSetMembers<PollAnswerDTO>(pollAnswerDto.PollId.ToString() + nameof(PollAnswerDTO));
			var poll = _redisService.Get<Poll>(pollAnswerDto.PollId.ToString());
			var pollResultsDto = GetPollResults(poll, pollAnswerDtos);
			// signal
			await _meetingHub.Clients.Group(poll.MeetingId.ToString()).SendAsync("OnPollResults", pollResultsDto);
		}

		private PollResultsDTO GetPollResults(Poll poll, ICollection<PollAnswerDTO> pollAnswerDtos)
		{
			var answerVariants = poll.Answers.ToList();

			var answerResults = pollAnswerDtos.Select(answer => answer.Answers);
			int totalChecked = answerResults.Select(array => array.Length).Sum();

			var pollResultsDto = _mapper.Map<PollResultsDTO>(poll);
			
			for (int i = 0; i < answerVariants.Count; i++)
			{
				int answerVotedCount = answerResults.Count(array => array.Contains(i));
				int percentage = answerVotedCount * 100 / totalChecked;
				pollResultsDto.Results.Add(answerVariants[i], percentage);
			}

			pollResultsDto.TotalVoted = answerResults.Count();

			return pollResultsDto;
		}
	}
}