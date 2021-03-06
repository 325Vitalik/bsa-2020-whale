import { PointAgenda } from '../agenda/agenda';
import { Recurrence } from './meeting-recurrence';

export interface MeetingCreate {
  topic?: string;
  description?: string;
  settings: string;
  startTime: Date;
  anonymousCount: number;
  isScheduled: boolean;
  isRecurrent: boolean;
  recurrence: Recurrence;
  isAudioAllowed: boolean;
  isVideoAllowed: boolean;
  isWhiteboard: boolean;
  isAllowedToChooseRoom: boolean;
  isPoll: boolean;
  creatorEmail: string;
  participantsEmails: string[];
  agendaPoints?: PointAgenda[];
  recognitionLanguage: string;
  selectMusic: string;
  meetingType: string;
}
