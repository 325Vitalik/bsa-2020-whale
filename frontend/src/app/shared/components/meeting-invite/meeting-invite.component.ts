import { Component, OnInit } from '@angular/core';
import { FormGroup, FormControl } from '@angular/forms';
import { HttpService } from 'app/core/services';
import { environment } from '@env';
import { ToastrService } from 'ngx-toastr';
import { SimpleModalComponent } from 'ngx-simple-modal';
import { MeetingInviteModalData } from '@shared/models/email/meeting-invite-modal-data';
import { MeetingInvite } from '@shared/models/email/meeting-invite';
import { Contact, User, Participant } from '@shared/models';

@Component({
  selector: 'app-meeting-invite',
  templateUrl: './meeting-invite.component.html',
  styleUrls: ['./meeting-invite.component.sass'],
})
export class MeetingInviteComponent
  extends SimpleModalComponent<MeetingInviteModalData, boolean>
  implements OnInit, MeetingInviteModalData {
  public emails: string[] = [];
  public contacts: Contact[];
  public cachedContacts: Contact[];
  public selectedContacts: Contact[] = [];
  participants: Participant[];

  public form: FormGroup;
  public formSearch: FormGroup;

  public isLoading = false;

  meetingId: string;
  senderId: string;
  inviteLink: string;

  constructor(private httpService: HttpService, private toastr: ToastrService) {
    super();
    this.form = new FormGroup({
      email: new FormControl(''),
    });

    this.formSearch = new FormGroup({
      contactSearch: new FormControl(''),
    });
  }

  ngOnInit(): void {
    this.getContacts();
  }

  public getContacts(): void {
    this.httpService
      .getRequest<Contact[]>(environment.apiUrl + '/api/Contacts/accepted')
      .subscribe(
        (response) => {
          console.log(response);
          const filteredContacts = response.filter(
            (c) =>
              !this.participants.find(
                (p) => p.user.email === c.secondMember.email
              )
          );
          this.contacts = Array.from(filteredContacts);
          this.cachedContacts = Array.from(filteredContacts);
        },
        (error) => {
          console.error(error);
        }
      );
  }

  public onContactClicked(contact: Contact): void {
    this.selectedContacts.push(contact);
    this.emails.push(contact.secondMember.email);
    this.cachedContacts.splice(this.cachedContacts.indexOf(contact), 1);
    this.contacts.splice(this.contacts.indexOf(contact), 1);
  }

  public addEmailTag(valid: boolean): void {
    if (valid) {
      const emailValue = this.form.controls.email.value.toLowerCase();
      const isParticipant =
        this.participants.find((p) => p.user.email === emailValue) !==
        undefined;
      if (isParticipant) {
        this.toastr.show(`${emailValue} is already participant of meeting.`);
      }
      if (this.emails.find((email) => email === emailValue)) {
        this.toastr.show(`${emailValue} is already added.`);
      }
      if (
        emailValue &&
        !this.emails.find((email) => email === emailValue) &&
        !isParticipant
      ) {
        this.emails.push(emailValue);
      }
      this.form.controls.email.setValue('');
    }
  }

  public removeTag(email: string): void {
    this.emails.splice(this.emails.indexOf(email), 1);
    const contact = this.selectedContacts.find(
      (c) => c.secondMember.email === email
    );
    if (contact) {
      this.cachedContacts.push(contact);
      this.contacts.push(contact);
      this.selectedContacts.splice(this.selectedContacts.indexOf(contact), 1);
    }
  }

  public sendInvites(): void {
    this.isLoading = true;
    const inviteData: MeetingInvite = {
      meetingLink: this.inviteLink,
      meetingId: this.meetingId,
      senderId: this.senderId,
      receiverEmails: this.emails,
    };

    this.httpService
      .postRequest<MeetingInvite, null>(
        environment.apiUrl + '/api/email',
        inviteData
      )
      .subscribe(
        () => {
          this.toastr.success('Invites have been sent.', 'Success');
          this.toastr.info(
            'Only members of Whale will receive invites',
            'Info'
          );
          setTimeout(() => this.closeModal(false), 1000);
        },
        (error) => {
          this.toastr.error(error.Message);
          console.error(error);
          this.isLoading = false;
        }
      );
  }

  public filterContacts(value: string): void {
    console.log(this.contacts);
    this.cachedContacts = this.contacts.filter((contact) => {
      return `${contact.secondMember.firstName} ${contact.secondMember.secondName}`.includes(
        value
      );
    });
  }

  public getEmailOrName(email: string): string {
    const contact = this.selectedContacts.find(
      (c) => c.secondMember.email === email
    );
    if (contact) {
      return this.getName(contact.secondMember);
    } else {
      return email;
    }
  }

  public getName(user: User): string {
    return user.secondName
      ? `${user.firstName} ${user.secondName}`
      : user.firstName;
  }

  public closeModal(result: boolean) {
    this.result = result; // result = isShowParticipants after modal closing
    this.close();
  }
}
