var CACHE_NAME = "get-a-pet-v3";
var SHELL_FILES = ["/", "/index.html", "/manifest.json"];
var PHOTO_CACHE = "get-a-pet-photos-v1";

self.addEventListener("install", function (event) {
  event.waitUntil(
    caches.open(CACHE_NAME).then(function (cache) {
      return cache.addAll(SHELL_FILES);
    })
  );
  self.skipWaiting();
});

self.addEventListener("activate", function (event) {
  event.waitUntil(
    caches.keys().then(function (names) {
      return Promise.all(
        names
          .filter(function (name) {
            return name !== CACHE_NAME && name !== PHOTO_CACHE;
          })
          .map(function (name) {
            return caches.delete(name);
          })
      );
    })
  );
  self.clients.claim();
});

self.addEventListener("fetch", function (event) {
  var url = new URL(event.request.url);

  if (url.pathname.startsWith("/api/")) {
    event.respondWith(
      fetch(event.request).catch(function () {
        return new Response(JSON.stringify({ offline: true }), {
          headers: { "Content-Type": "application/json" },
        });
      })
    );
    return;
  }

  if (
    event.request.destination === "image" &&
    url.hostname !== location.hostname
  ) {
    event.respondWith(
      caches.open(PHOTO_CACHE).then(function (cache) {
        return cache.match(event.request).then(function (cached) {
          if (cached) {
            return cached;
          }
          return fetch(event.request).then(function (response) {
            if (response.ok) {
              cache.put(event.request, response.clone());
            }
            return response;
          });
        });
      })
    );
    return;
  }

  event.respondWith(
    fetch(event.request)
      .then(function (response) {
        if (response.ok) {
          caches.open(CACHE_NAME).then(function (cache) {
            cache.put(event.request, response.clone());
          });
        }
        return response;
      })
      .catch(function () {
        return caches.match(event.request);
      })
  );
});

self.addEventListener("push", function (event) {
  var data = { title: "get-a-pet", body: "New update available" };

  if (event.data) {
    try {
      data = event.data.json();
    } catch (e) {
      data.body = event.data.text();
    }
  }

  event.waitUntil(
    self.registration.showNotification(data.title, {
      body: data.body,
      icon: data.icon || "/icon-192.png",
      image: data.icon || undefined,
      badge: "/badge-72.png",
      data: data.data || {},
    })
  );
});

self.addEventListener("notificationclick", function (event) {
  event.notification.close();

  var url = event.notification.data && event.notification.data.url;
  if (!url) {
    url = "/";
  }

  event.waitUntil(
    self.clients.matchAll({ type: "window" }).then(function (clients) {
      for (var i = 0; i < clients.length; i++) {
        if (clients[i].url === url && "focus" in clients[i]) {
          return clients[i].focus();
        }
      }
      return self.clients.openWindow(url);
    })
  );
});
