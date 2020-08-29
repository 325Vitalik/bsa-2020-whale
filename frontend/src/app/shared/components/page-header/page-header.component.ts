import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from 'app/core/auth/auth.service';
import { Notification } from 'app/shared/models/notification/notification';
import { User } from '@shared/models/user';
import { tap, takeUntil } from 'rxjs/operators';
import { UpstateService } from '../../../core/services/upstate.service';
import { NotificationService } from 'app/core/services/notification.service';
import { Subject } from 'rxjs';
import { WhaleSignalService, WhaleSignalMethods } from 'app/core/services';

@Component({
  selector: 'app-page-header',
  templateUrl: './page-header.component.html',
  styleUrls: ['./page-header.component.sass'],
})
export class PageHeaderComponent implements OnInit, OnDestroy {
  public isUserLoadig = true;
  private unsubscribe$ = new Subject<void>();

  settingsMenuVisible = false;
  isNotificationsVisible = false;
  loggedInUser: User;

  public notificationsList: Notification[];

  constructor(
    private router: Router,
    public auth: AuthService,
    private upstateService: UpstateService,
    private notificationService: NotificationService,
    private whaleSignalrService: WhaleSignalService
  ) {}

  public showNotificationsMenu(): void {
    if (this.notificationsList.length) {
      if (this.settingsMenuVisible) {
        this.settingsMenuVisible = false;
      }

      window.onclick = null;
      this.isNotificationsVisible = !this.isNotificationsVisible;

      if (this.isNotificationsVisible) {
        window.onclick = () => {
          this.isNotificationsVisible = false;
        };
      }
    }
  }

  public showSettingsMenu(): void {
    if (this.isNotificationsVisible) {
      this.isNotificationsVisible = false;
    }

    window.onclick = null;
    this.settingsMenuVisible = !this.settingsMenuVisible;

    if (this.settingsMenuVisible) {
      window.onclick = () => {
        this.settingsMenuVisible = false;
      };
    }
  }

  ngOnInit(): void {
    this.getUser();
    this.getNotifications();
    this.subscribeNotifications();
  }

  ngOnDestroy(): void {
    this.unsubscribe$.next();
    this.unsubscribe$.complete();
  }

  getUser(): void {
    this.upstateService
      .getLoggedInUser()
      .pipe(tap(() => (this.isUserLoadig = false)))
      .subscribe((userFromDB: User) => {
        this.loggedInUser = userFromDB;
      });
  }
  getNotifications(): void {
    this.notificationService.GetNotifications().subscribe((notifications) => {
      this.notificationsList = notifications;
    });
  }

  subscribeNotifications(): void {
    this.whaleSignalrService.receiveNotify$
      .pipe(takeUntil(this.unsubscribe$))
      .subscribe(
        (newNotification) => {
          this.notificationsList.push(newNotification);
        },
        (err) => {
          console.log(err.message);
        }
      );

    this.whaleSignalrService.updateNotify$
      .pipe(takeUntil(this.unsubscribe$))
      .subscribe(
        (updateNotification) => {
          const index = this.notificationsList.findIndex(
            (n) => n.id === updateNotification.id
          );
          if (index >= 0) {
            this.notificationsList[index] = updateNotification;
          }
        },
        (err) => {
          console.log(err.message);
        }
      );

    this.whaleSignalrService.removeNotify$
      .pipe(takeUntil(this.unsubscribe$))
      .subscribe(
        (notificationId) => {
          this.notificationsList = this.notificationsList.filter(
            (n) => n.id !== notificationId
          );
          if (!this.notificationsList.length) {
            this.showNotificationsMenu();
          }
        },
        (err) => {
          console.log(err.message);
        }
      );
  }

  goToPage(pageName: string): void {
    this.router.navigate([`${pageName}`]);
  }

  logOut(): void {
    this.whaleSignalrService.invoke(
      WhaleSignalMethods.OnUserDisconnect,
      this.loggedInUser.email
    );
    this.auth.logout().subscribe(() => this.router.navigate(['landing']));
  }

  onNotificationDelete(id: string): void {
    this.notificationsList = this.notificationsList.filter((n) => n.id !== id);
    this.notificationService.DeleteNotification(id);
    if (!this.notificationsList.length) {
      this.showNotificationsMenu();
    }
  }
}
