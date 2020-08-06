import { Component } from '@angular/core';
import { WebrtcSignalService, SignalData } from './core/services/webrtc-signal.service';
import { AuthService } from './core/auth/auth.service';
import { HttpClient } from '@angular/common/http';


@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.sass']
})
export class AppComponent {
  title = 'frontend';
  constructor(
    public fireAuth: AuthService,
    private http: HttpClient
    )
  {}
}
