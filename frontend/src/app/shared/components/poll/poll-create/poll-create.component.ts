import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { FormGroup, FormControl, Validators, FormArray } from '@angular/forms';
import { PollCreateDto } from '../../../models/poll/poll-create-dto';
import { HttpService } from 'app/core/services/http.service';
import { ToastrService } from 'ngx-toastr';
import { environment } from '@env';
import { PollDto } from '@shared/models/poll/poll-dto';

@Component({
  selector: 'app-poll-create',
  templateUrl: './poll-create.component.html',
  styleUrls: ['./poll-create.component.sass'],
})
export class PollCreateComponent implements OnInit {
  @Input() meetingId: string;
  @Output() pollCreated = new EventEmitter<PollDto>();
  @Output() pollCreateClosed = new EventEmitter<void>();

  public isLoading = false;

  public form: FormGroup;

  constructor(private httpService: HttpService, private toastr: ToastrService) {
    this.form = new FormGroup({
      title: new FormControl('', Validators.required),
      isAnonymous: new FormControl(false),
      isSingleChoise: new FormControl(false),
      options: new FormArray([
        new FormControl('', Validators.required),
        new FormControl('', Validators.required),
      ]),
    });
  }

  get options(): FormArray {
    return this.form.get('options') as FormArray;
  }

  addOption(): void {
    if (this.options.length === 5) {
      this.toastr.warning('Maximum 5 options', 'Warning');
      return;
    }
    this.options.push(new FormControl('', Validators.required));
  }

  removeOption(event: MouseEvent): void {
    this.options.removeAt(Number((event.target as HTMLSpanElement).id));
  }

  closePollCreate(): void {
    this.pollCreateClosed.emit();
  }

  ngOnInit(): void {}

  public onSubmit(): void {
    this.isLoading = true;
    const options = this.options.controls.map((ctrl) => ctrl.value);

    if (new Set(options).size !== options.length) {
      this.toastr.error('Options must be unique', 'Error');
      this.isLoading = false;
      return;
    }

    const pollCreateDto: PollCreateDto = {
      meetingId: this.meetingId,
      title: this.form.controls.title.value,
      isAnonymous: this.form.controls.isAnonymous.value,
      isSingleChoice: this.form.controls.isSingleChoise.value,
      options: this.options.controls.map((ctrl) => ctrl.value),
    };

    this.httpService
      .postRequest<PollCreateDto, PollDto>(
        environment.apiUrl + '/polls',
        pollCreateDto
      )
      .subscribe(
        (response: PollDto) => {
          this.isLoading = true;
          this.pollCreated.emit(response);
        },
        (error) => {
          this.isLoading = false;
          this.toastr.error(error);
        }
      );
  }
}
