import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PageHeaderComponent } from './components/page-header/page-header.component';
import { PollCreateComponent } from './components/poll/poll-create/poll-create.component';
import { ReactiveFormsModule } from '@angular/forms';
import { PollComponent } from './components/poll/poll/poll.component';
import { SpinerComponent } from './components/spiner/spiner.component';
import { PollResultsComponent } from './components/poll/poll-results/poll-results.component';
import { FormsModule } from '@angular/forms';
import { DateFormatPipe } from './pipes/date-format.pipe';
import { ParticipantCardComponent } from './components/participant-card/participant-card.component';
import { MeetingStatisticsComponent } from './components/meeting-statistics/meeting-statistics.component';
import { CopyClipboardComponent } from './components/copy-clipboard/copy-clipboard.component';
import { IncomingCallComponent } from './components/incoming-call/incoming-call.component';
import { AngularDraggableModule } from 'angular2-draggable';
import { ProgressBarDirective } from './directives/progress-bar.directive';
import { AudioSettingsComponent } from '@shared/components/settings/audio-settings/audio-settings.component';
import { VideoSettingsComponent } from '@shared/components/settings/video-settings/video-settings.component';
import { OutputSettingsComponent } from './components/settings/output-settings/output-settings.component';
import { NotificationComponent } from './components/notification/notification.component';
import { IncomingGroupCallComponent } from './components/incoming-group-call/incoming-group-call.component';
import { MeetingInviteComponent } from './components/meeting-invite/meeting-invite.component';
import { TagComponent } from './components/meeting-invite/tag/tag.component';
import { ConfirmationModalComponent } from './components/confirmation-modal/confirmation-modal.component';
import { ContactComponent } from './components/contacts/contact/contact.component';
import { QuestionComponent } from './components/question/question/question.component';
import { QuestionStatusDirective } from './directives/question-status.directive';
import { DndModule } from 'ngx-drag-drop';
import { SortContactsPipe } from './pipes/sort-contacts.pipe';
import { GroupHostOnTopPipe } from './pipes/group-host-on-top.pipe';
import { KeysPipe } from './pipes/keys.pipe';

@NgModule({
  declarations: [
    PageHeaderComponent,
    SpinerComponent,
    DateFormatPipe,
    KeysPipe,
    ParticipantCardComponent,
    PollCreateComponent,
    PollComponent,
    PollResultsComponent,
    MeetingStatisticsComponent,
    CopyClipboardComponent,
    IncomingCallComponent,
    ProgressBarDirective,
    AudioSettingsComponent,
    VideoSettingsComponent,
    OutputSettingsComponent,
    NotificationComponent,
    IncomingGroupCallComponent,
    MeetingInviteComponent,
    TagComponent,
    ConfirmationModalComponent,
    ContactComponent,
    QuestionComponent,
    QuestionStatusDirective,
    SortContactsPipe,
    GroupHostOnTopPipe,
  ],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    AngularDraggableModule,
    DndModule,
  ],
  exports: [
    CommonModule,
    PageHeaderComponent,
    PollCreateComponent,
    PollComponent,
    PollResultsComponent,
    SpinerComponent,
    DateFormatPipe,
    KeysPipe,
    ParticipantCardComponent,
    MeetingStatisticsComponent,
    IncomingCallComponent,
    IncomingGroupCallComponent,
    AudioSettingsComponent,
    VideoSettingsComponent,
    OutputSettingsComponent,
    TagComponent,
    QuestionComponent,
    DndModule,
    SortContactsPipe,
    GroupHostOnTopPipe,
  ],
})
export class SharedModule {}
