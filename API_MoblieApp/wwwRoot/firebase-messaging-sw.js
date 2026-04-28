importScripts('https://www.gstatic.com/firebasejs/8.10.1/firebase-app.js');
importScripts('https://www.gstatic.com/firebasejs/8.10.1/firebase-messaging.js');

firebase.initializeApp({
    apiKey: "AIzaSyBsDId9RZGs2PqhdzNxBLi6IfnmE4mbdMM",
    authDomain: "sirs-e3927.firebaseapp.com",
    projectId: "sirs-e3927",
    storageBucket: "sirs-e3927.firebasestorage.app",
    messagingSenderId: "74861986359",
    appId: "1:74861986359:web:6e6d94f01cd389cae535b2"
});

const messaging = firebase.messaging();

// معالج الرسائل في الخلفية (إجباري لمنع أخطاء التسجيل)
messaging.onBackgroundMessage((payload) => {
    console.log('[firebase-messaging-sw.js] Received background message ', payload);

    const notificationTitle = payload.notification.title;
    const notificationOptions = {
        body: payload.notification.body,
        icon: '/favicon.ico' // تأكد من وجود أيقونة بهذا الاسم في wwwroot
    };

    self.registration.showNotification(notificationTitle, notificationOptions);
});