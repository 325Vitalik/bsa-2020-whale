﻿using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Whale.Shared.Helpers;
using Whale.Shared.Services.Abstract;
using Whale.DAL;
using Whale.Shared.Exceptions;
using Whale.DAL.Models;
using Whale.Shared.Models.Meeting;
using Whale.Shared.Models.Participant;
using Whale.Shared.Models.Meeting.MeetingMessage;
using shortid;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using Whale.DAL.Models.Email;
using Whale.Shared.Models.Email;
using System.Net.Http.Headers;
using Whale.Shared.Models;
using Microsoft.Extensions.Configuration;
using Whale.Shared.Models.Statistics;


namespace Whale.Shared.Services
{
    public class MeetingService : BaseService
    {
        private const string meetingSettingsPrefix = "meeting-settings-";
        private const string meetingSpeechPrefix = "meeting-speech-";
        private readonly RedisService _redisService;
        private readonly UserService _userService;
        private readonly ParticipantService _participantService;
        private readonly EncryptHelper _encryptService;
        private readonly SignalrService _signalrService;
        private readonly NotificationsService _notifications;
        private readonly string whaleAPIurl;
        private readonly ElasticSearchService _elasticSearchService;

        public static string BaseUrl { get; } = "http://bsa2020-whale.westeurope.cloudapp.azure.com";

        public MeetingService(
            WhaleDbContext context,
            IMapper mapper,
            RedisService redisService,
            UserService userService,
            ParticipantService participantService,
            EncryptHelper encryptService,
            SignalrService signalrService,
            IConfiguration configuration,
            NotificationsService notifications,
            ElasticSearchService elasticSearchService)
            : base(context, mapper)
        {
            _redisService = redisService;
            _userService = userService;
            _participantService = participantService;
            _encryptService = encryptService;
            _signalrService = signalrService;
            _notifications = notifications;
            whaleAPIurl = configuration.GetValue<string>("Whale");
            _elasticSearchService = elasticSearchService;
        }

        public async Task<MeetingDTO> ConnectToMeeting(MeetingLinkDTO linkDTO, string userEmail)
        {
            await _redisService.ConnectAsync();
            var redisDTO = await _redisService.GetAsync<MeetingMessagesAndPasswordDTO>(linkDTO.Id.ToString());
            Console.WriteLine($"MeetingId: {linkDTO.Id}\nrdisDTO: {redisDTO is null}");
            if (redisDTO?.Password != linkDTO.Password)
                throw new InvalidCredentialsExseption();

            var meeting = await _context.Meetings.FirstOrDefaultAsync(m => m.Id == linkDTO.Id);
            if (meeting == null)
                throw new NotFoundException("Meeting");

            if ((await _participantService.GetMeetingParticipantByEmail(meeting.Id, userEmail)) == null)
            {
                await _participantService.CreateParticipantAsync(new ParticipantCreateDTO
                {
                    Role = ParticipantRole.Participant,
                    UserEmail = userEmail,
                    MeetingId = meeting.Id
                });
            }

            var meetingSettings = await _redisService.GetAsync<MeetingSettingsDTO>($"{meetingSettingsPrefix}{linkDTO.Id}");

            var meetingDTO = _mapper.Map<MeetingDTO>(meeting);
            meetingDTO.Participants = (await _participantService.GetMeetingParticipantsAsync(meeting.Id)).ToList();
            meetingDTO.IsAudioAllowed = meetingSettings.IsAudioAllowed;
            meetingDTO.IsVideoAllowed = meetingSettings.IsVideoAllowed;
            meetingDTO.IsPoll = meetingSettings.IsPoll;
            meetingDTO.IsWhiteboard = meetingSettings.IsWhiteboard;
            meetingDTO.IsAllowedToChooseRoom = meetingSettings.IsAllowedToChooseRoom;
            meetingDTO.RecognitionLanguage = meetingSettings.RecognitionLanguage;

            return meetingDTO;
        }

        public async Task<MeetingLinkDTO> CreateMeeting(MeetingCreateDTO meetingDTO)
        {
            var meeting = _mapper.Map<Meeting>(meetingDTO);
            if (!meeting.IsScheduled)
            {
                meeting.StartTime = DateTimeOffset.Now;
            }

            await _context.Meetings.AddAsync(meeting);
            await _context.SaveChangesAsync();

            await _redisService.ConnectAsync();

            var pwd = _encryptService.EncryptString(Guid.NewGuid().ToString());
            await _redisService.SetAsync(meeting.Id.ToString(), new MeetingMessagesAndPasswordDTO { Password = pwd, MeetingId = meeting.Id.ToString() });
            await _redisService.SetAsync($"{meetingSettingsPrefix}{meeting.Id}", new MeetingSettingsDTO
            {
                MeetingHostEmail = meetingDTO.CreatorEmail,
                IsAudioAllowed = meetingDTO.IsAudioAllowed,
                IsVideoAllowed = meetingDTO.IsVideoAllowed,
                IsWhiteboard = meetingDTO.IsWhiteboard,
                IsAllowedToChooseRoom = meetingDTO.IsAllowedToChooseRoom,
                IsPoll = meetingDTO.IsPoll,
                RecognitionLanguage = meetingDTO.RecognitionLanguage
    });

            string shortURL = ShortId.Generate();
            string fullURL = $"?id={meeting.Id}&pwd={pwd}";
            await _redisService.SetAsync(fullURL, shortURL);
            await _redisService.SetAsync(shortURL, fullURL);

            await _participantService.CreateParticipantAsync(new ParticipantCreateDTO
            {
                Role = ParticipantRole.Host,
                UserEmail = meetingDTO.CreatorEmail,
                MeetingId = meeting.Id
            });

            return new MeetingLinkDTO { Id = meeting.Id, Password = pwd };
        }

        public async Task<MeetingAndLink> RegisterScheduledMeeting(MeetingCreateDTO meetingDTO)
        {
            var meeting = _mapper.Map<Meeting>(meetingDTO);
            meeting.Settings = JsonConvert.SerializeObject(new
            {
                meetingDTO.IsAudioAllowed,
                meetingDTO.IsVideoAllowed,
                meetingDTO.IsAllowedToChooseRoom,
                meetingDTO.IsPoll,
                meetingDTO.IsWhiteboard,
                meetingDTO.RecognitionLanguage,
            });
            await _context.Meetings.AddAsync(meeting);
            var user = await _context.Users.FirstOrDefaultAsync(e => e.Email == meetingDTO.CreatorEmail);
            var pwd = _encryptService.EncryptString(Guid.NewGuid().ToString());
            var shortURL = ShortId.Generate();
            var fullURL = $"?id={meeting.Id}&pwd={pwd}";
            var scheduledMeeting = new ScheduledMeeting
            {
                CreatorId = user.Id,
                MeetingId = meeting.Id,
                ParticipantsEmails = JsonConvert.SerializeObject(meetingDTO.ParticipantsEmails),
                Password = pwd,
                ShortURL = shortURL,
                FullURL = fullURL
            };
            await _context.ScheduledMeetings.AddAsync(scheduledMeeting);
            await _context.SaveChangesAsync();

            using (var client = new HttpClient())
            {
                var meetingInvite = new ScheduledMeetingInvite
                {
                    MeetingLink = fullURL,
                    MeetingId = meeting.Id,
                    ReceiverEmails = meetingDTO.ParticipantsEmails
                };
                (meetingInvite.ReceiverEmails as List<string>).Add(user.Email);
                client.PostAsync(whaleAPIurl + "/email/scheduled", new StringContent(JsonConvert.SerializeObject(meetingInvite), Encoding.UTF8, "application/json"));
            }
            await _redisService.ConnectAsync();
            await _redisService.SetAsync(shortURL, "not-active");

            return new MeetingAndLink { Meeting = meeting , Link = shortURL };
        }

        public async Task<MeetingLinkDTO> StartScheduledMeeting(Meeting meeting)
        {
            var meetingSettings = JsonConvert.DeserializeObject(meeting.Settings);
            var scheduledMeeing = await _context.ScheduledMeetings.FirstOrDefaultAsync(e => e.MeetingId == meeting.Id);
            await _redisService.ConnectAsync();
            await _redisService.SetAsync(meeting.Id.ToString(), new MeetingMessagesAndPasswordDTO { Password = scheduledMeeing.Password, MeetingId = meeting.Id.ToString() });
            await _redisService.SetAsync($"{meetingSettingsPrefix}{meeting.Id}", new MeetingSettingsDTO
            {
                IsAudioAllowed = ((dynamic)meetingSettings).IsAudioAllowed,
                IsVideoAllowed = ((dynamic)meetingSettings).IsVideoAllowed,
            });

            await _redisService.SetAsync(scheduledMeeing.FullURL, scheduledMeeing.ShortURL);
            await _redisService.SetAsync(scheduledMeeing.ShortURL, scheduledMeeing.FullURL);

            var scheduledMeeting = await _context.ScheduledMeetings.FirstOrDefaultAsync(e => e.MeetingId == meeting.Id);
            var user = await _context.Users.FirstOrDefaultAsync(e => e.Id == scheduledMeeting.CreatorId);
            await _participantService.CreateParticipantAsync(new ParticipantCreateDTO
            {
                Role = ParticipantRole.Host,
                UserEmail = user.Email,
                MeetingId = meeting.Id
            });

            var participantEmails = JsonConvert.DeserializeObject<List<string>>(scheduledMeeting.ParticipantsEmails);

            var link = $"{BaseUrl}/redirection/{scheduledMeeing.ShortURL}";

            foreach (var email in participantEmails)
            {
                var userParticipant = await _userService.GetUserByEmail(email);
                if (userParticipant == null)
                    continue;
                await _notifications.InviteMeetingNotification(user.Email, email, link);
            }

            return new MeetingLinkDTO { Id = meeting.Id, Password = scheduledMeeing.Password };
        }

        public async Task<IEnumerable<Meeting>> GetScheduledMeetins()
        {
            return await _context.Meetings
                .Where(e => e.IsScheduled && e.EndTime == null && e.StartTime >= DateTimeOffset.Now)
                .ToListAsync();
        }

        public async Task UpdateMeetingSettings(UpdateSettingsDTO updateSettingsDTO)
        {
            await _redisService.ConnectAsync();

            var meetingSettings =
                await _redisService.GetAsync<MeetingSettingsDTO>($"{meetingSettingsPrefix}{updateSettingsDTO.MeetingId}");

            if (meetingSettings == null)
                throw new NotFoundException("meeting settings");

            if (updateSettingsDTO.ApplicantEmail != meetingSettings.MeetingHostEmail)
                throw new NotAllowedException(updateSettingsDTO.ApplicantEmail);

            meetingSettings.IsWhiteboard = updateSettingsDTO.IsWhiteboard;
            meetingSettings.IsPoll = updateSettingsDTO.IsPoll;
            meetingSettings.IsAudioAllowed = !updateSettingsDTO.IsAudioDisabled;
            meetingSettings.IsVideoAllowed = !updateSettingsDTO.IsVideoDisabled;
            meetingSettings.IsAllowedToChooseRoom = updateSettingsDTO.IsAllowedToChooseRoom;
            meetingSettings.RecognitionLanguage = updateSettingsDTO.RecognitionLanguage;

            await _redisService.SetAsync($"{meetingSettingsPrefix}{updateSettingsDTO.MeetingId}", meetingSettings);
        }

        public async Task<MeetingMessageDTO> SendMessage(MeetingMessageCreateDTO msgDTO)
        {
            var message = _mapper.Map<MeetingMessageDTO>(msgDTO);
            message.SentDate = DateTimeOffset.Now;
            message.Id = Guid.NewGuid().ToString();

            var user = await _userService.GetUserByEmail(msgDTO.AuthorEmail);
            message.Author = user ?? throw new NotFoundException("User");

            if (!string.IsNullOrEmpty(msgDTO.ReceiverEmail))
            {
                var receiver = await _userService.GetUserByEmail(msgDTO.ReceiverEmail);
                message.Receiver = receiver ?? throw new NotFoundException("User");
            }

            await _redisService.ConnectAsync();
            var redisDTO = await _redisService.GetAsync<MeetingMessagesAndPasswordDTO>(msgDTO.MeetingId);
            redisDTO.Messages.Add(message);
            await _redisService.SetAsync(msgDTO.MeetingId, redisDTO);

            return message;
        }

        public async Task<IEnumerable<MeetingMessageDTO>> GetMessagesAsync(string groupName, string userEmail)
        {
            await _redisService.ConnectAsync();
            var redisDTO = await _redisService.GetAsync<MeetingMessagesAndPasswordDTO>(groupName);
            return  redisDTO.Messages
                .Where(m => m.Receiver == null || m.Author.Email == userEmail || m.Receiver.Email == userEmail);
        }

        public async Task ParticipantDisconnect(string groupname, string userEmail)
        {
            var participant = await _participantService.GetMeetingParticipantByEmail(Guid.Parse(groupname), userEmail);
            if (participant == null)
                throw new NotFoundException("Participant");
        }

        public async Task EndMeeting(Guid meetingId)
        {
            var meeting = await _context.Meetings
                .Include(m => m.Participants)
                .FirstOrDefaultAsync(m => m.Id == meetingId);

            if (meeting == null)
            {
                throw new NotFoundException(nameof(Meeting));
            }
            await _redisService.ConnectAsync();
            var redisMeetingScript = await _redisService.GetAllListJson($"{meetingSpeechPrefix}{meetingId}");
            var meetingScript = new MeetingScript
            {
                MeetingId = meeting.Id,
                Script = redisMeetingScript,
            };
            _context.MeetingScripts.Add(meetingScript);
            await _context.SaveChangesAsync();

            var redisMeetingData = await _redisService.GetAsync<MeetingMessagesAndPasswordDTO>(meetingId.ToString());
            await _redisService.RemoveAsync(meetingId.ToString());

            string fullURL = $"?id={meetingId}&pwd={redisMeetingData.Password}";
            var shortUrl = await _redisService.GetAsync<string>(fullURL);
            await _redisService.RemoveAsync(fullURL);
            await _redisService.RemoveAsync(shortUrl);
            await _redisService.RemoveAsync($"{meetingSettingsPrefix}{meetingId}");
            await _redisService.RemoveAsync($"{meetingSpeechPrefix}{meetingId}");

            meeting.EndTime = DateTimeOffset.Now;
            _context.Update(meeting);

            await _context.SaveChangesAsync();
            await _notifications.UpdateInviteMeetingNotifications(shortUrl);
            foreach(var p in meeting.Participants)
            {
                var statistics = new MeetingUserStatistics
                {
                    Id = $"{p.UserId.ToString()}{p.MeetingId.ToString()}",
                    UserId = p.UserId,
                    MeetingId = meeting.Id,
                    StartDate = meeting.StartTime,
                    EndDate = (DateTimeOffset)meeting.EndTime,
                    PresenceTime = 0,
                    DurationTime = (long)((DateTimeOffset)meeting.EndTime).Subtract(meeting.StartTime).TotalSeconds,
                    SpeechTime = 0
                };
                await _elasticSearchService.SaveSingleAsync(statistics);
            }
        }

        public async Task ReloadStatistics()
        {
            var users = _context.Users.ToList();
            foreach(var user in users)
            {
                var statistics = _context.Participants
                .Include(p => p.Meeting)
                .Where(p => p.UserId == user.Id && p.Meeting.EndTime.HasValue)
                .Select(p => new MeetingUserStatistics
                {
                    Id = $"{p.UserId.ToString()}{p.MeetingId.ToString()}",
                    UserId = p.UserId,
                    MeetingId = p.MeetingId,
                    StartDate = p.Meeting.StartTime,
                    EndDate = (DateTimeOffset)p.Meeting.EndTime,
                    PresenceTime = 0,
                    DurationTime = (long)((DateTimeOffset)p.Meeting.EndTime).Subtract(p.Meeting.StartTime).TotalSeconds,
                    SpeechTime = 0
                })
                .AsEnumerable();
                await _elasticSearchService.SaveRangeAsync(statistics);
            }

            
        }

        public async Task<string> GetShortInviteLink(string id, string pwd)
        {
            await _redisService.ConnectAsync();
            return await _redisService.GetAsync<string>($"?id={id}&pwd={pwd}");
        }

        public async Task<string> GetFullInviteLink(string shortURL)
        {
            await _redisService.ConnectAsync();
            return await _redisService.GetAsync<string>(shortURL);
        }

        public async Task SpeechRecognition(MeetingSpeechCreateDTO speechDTO)
        {
            var speech = new MeetingSpeech
            {
                UserId = speechDTO.UserId,
                Message = speechDTO.Message,
                SpeechDate = DateTimeOffset.Now
            };
            await _redisService.ConnectAsync();
            await _redisService.AddToList($"{meetingSpeechPrefix}{speechDTO.MeetingId}", speech);

            return;
        }
        public async Task<List<AgendaPointDTO>> GetAgendaPoints(string meetingId)
        {
            return _mapper.Map<List<AgendaPointDTO>>(_context.AgendaPoints.Where(x => x.MeetingId == Guid.Parse(meetingId)).ToList());
        } 
        public async Task UpdateTopic(AgendaPointDTO point)
        {
            point.StartTime.AddMinutes(5);
            _context.AgendaPoints.Update(_mapper.Map<AgendaPoint>(point));
            await _context.SaveChangesAsync();
        }
    }
}
