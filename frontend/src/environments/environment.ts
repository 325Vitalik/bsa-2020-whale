// This file can be replaced during build by using the `fileReplacements` array.
// `ng build --prod` replaces `environment.ts` with `environment.prod.ts`.
// The list of file replacements can be found in `angular.json`.
export const environment = {
  production: false,
  apiUrl: 'http://localhost:4201',
  meetingApiUrl: 'http://localhost:4202',
  signalrUrl: 'http://localhost:4203',
  firebase: {
    apiKey: 'AIzaSyCdb8h9YMeJBdcIM5wK_w6Mcw7ZI1CVJAk',
    authDomain: 'bsa-whale.firebaseapp.com',
    databaseURL: 'https://bsa-whale.firebaseio.com',
    projectId: 'bsa-whale',
    storageBucket: 'bsa-whale.appspot.com',
    messagingSenderId: '893944865679',
    appId: '1:893944865679:web:9b055d730e3a27b66961fa',
  },
  googleClientId:
    '893944865679-eav20gfr3sbintikhq42dhhc414loq4p.apps.googleusercontent.com',
  peerOptions: {
    // ! default settings
    // key: 'peerjs',
    // host: '0.peerjs.com',
    // port: 443,
    // path: '/',
    // secure: true,
    // debug: 1,

    // ! remote server settings
    key: 'peerjs',
    host: 'whale-peerjs-server.herokuapp.com',
    port: 443,
    secure: true,
    path: '/',
    debug: 1,

    // ! addtional
    // ssl:{
    //   key: "",
    //   certificate: ""
    // },
    config: {
      iceServers: [
        { urls: 'stun:stun1.l.google.com:19302' },
        { urls: 'stun:stun2.l.google.com:19302' },
        {
          urls: 'turn:192.158.29.39:3478?transport=udp',
          credential: 'JZEOEt2V3Qb0y27GRntt2u2PAYA=',
          username: '28224511:1379330808',
        },
        {
          urls: 'turn:192.158.29.39:3478?transport=tcp',
          credential: 'JZEOEt2V3Qb0y27GRntt2u2PAYA=',
          username: '28224511:1379330808',
        },
      ],
    },
  },
};

/*
 * For easier debugging in development mode, you can import the following file
 * to ignore zone related error stack frames such as `zone.run`, `zoneDelegate.invokeTask`.
 *
 * This import should be commented out in production mode because it will have a negative impact
 * on performance if an error is thrown.
 */
// import 'zone.js/dist/zone-error';  // Included with Angular CLI.
