var API_BASE =
  location.hostname === "localhost" || location.hostname === "127.0.0.1"
    ? "http://localhost:5000"
    : "";

var DB_NAME = "khs-dog-monitor";
var DB_VERSION = 1;
var STORE_NAME = "status";
var POLL_INTERVAL = 30000;

var pollTimer = null;

function openDb() {
  return new Promise(function (resolve, reject) {
    var req = indexedDB.open(DB_NAME, DB_VERSION);
    req.onupgradeneeded = function () {
      req.result.createObjectStore(STORE_NAME);
    };
    req.onsuccess = function () {
      resolve(req.result);
    };
    req.onerror = function () {
      reject(req.error);
    };
  });
}

function saveToDb(data) {
  return openDb().then(function (db) {
    return new Promise(function (resolve, reject) {
      var tx = db.transaction(STORE_NAME, "readwrite");
      tx.objectStore(STORE_NAME).put(data, "latest");
      tx.oncomplete = function () {
        resolve();
      };
      tx.onerror = function () {
        reject(tx.error);
      };
    });
  });
}

function loadFromDb() {
  return openDb().then(function (db) {
    return new Promise(function (resolve, reject) {
      var tx = db.transaction(STORE_NAME, "readonly");
      var req = tx.objectStore(STORE_NAME).get("latest");
      req.onsuccess = function () {
        resolve(req.result || null);
      };
      req.onerror = function () {
        reject(req.error);
      };
    });
  });
}

function timeAgo(dateStr) {
  var ms = Date.now() - new Date(dateStr).getTime();
  var seconds = Math.floor(ms / 1000);
  if (seconds < 60) {
    return "just now";
  }
  var minutes = Math.floor(seconds / 60);
  if (minutes < 60) {
    return minutes + "m ago";
  }
  var hours = Math.floor(minutes / 60);
  if (hours < 24) {
    return hours + "h ago";
  }
  var days = Math.floor(hours / 24);
  return days + "d ago";
}

function renderDogs(dogs) {
  var grid = document.getElementById("dog-grid");
  var empty = document.getElementById("empty-state");

  if (!dogs || dogs.length === 0) {
    grid.innerHTML = "";
    empty.hidden = false;
    return;
  }

  empty.hidden = true;
  grid.innerHTML = dogs
    .map(function (dog) {
      var imgSrc = dog.photoUrl || "";
      var imgTag = imgSrc
        ? '<img src="' + imgSrc + '" alt="' + (dog.name || "Dog") + '" loading="lazy">'
        : '<img src="" alt="No photo" style="background:var(--color-border)">';

      return (
        '<div class="dog-card" data-aid="' +
        dog.aid +
        '">' +
        imgTag +
        '<div class="dog-card-info">' +
        "<h3>" +
        (dog.name || "Unknown") +
        "</h3>" +
        "<p>" +
        [dog.gender, dog.age].filter(Boolean).join(" / ") +
        "</p>" +
        (dog.breed ? "<p>" + dog.breed + "</p>" : "") +
        "</div></div>"
      );
    })
    .join("");

  grid.querySelectorAll(".dog-card").forEach(function (card) {
    card.addEventListener("click", function () {
      var aid = card.dataset.aid;
      var dog = dogs.find(function (d) {
        return d.aid === aid;
      });
      if (dog) {
        showModal(dog);
      }
    });
  });
}

function showModal(dog) {
  var overlay = document.getElementById("modal-overlay");
  document.getElementById("modal-img").src = dog.photoUrl || "";
  document.getElementById("modal-img").alt = dog.name || "Dog";
  document.getElementById("modal-name").textContent = dog.name || "Unknown";

  var details = [];
  if (dog.gender) {
    details.push({ label: "Gender", value: dog.gender });
  }
  if (dog.age) {
    details.push({ label: "Age", value: dog.age });
  }
  if (dog.breed) {
    details.push({ label: "Breed", value: dog.breed });
  }

  document.getElementById("modal-details").innerHTML = details
    .map(function (d) {
      return (
        '<div class="modal-detail"><span class="label">' +
        d.label +
        '</span><span>' +
        d.value +
        "</span></div>"
      );
    })
    .join("");

  var link = document.getElementById("modal-link");
  if (dog.profileUrl) {
    link.href = dog.profileUrl;
    link.hidden = false;
  } else {
    link.hidden = true;
  }

  overlay.classList.add("active");
}

function closeModal() {
  document.getElementById("modal-overlay").classList.remove("active");
}

function updateStatus(data, fromCache) {
  var indicator = document.getElementById("status-indicator");
  var statusText = document.getElementById("status-text");
  var lastChecked = document.getElementById("last-checked");
  var countBadge = document.getElementById("count-badge");
  var cacheAge = document.getElementById("cache-age");

  if (data.isMonitoringActive) {
    indicator.classList.remove("inactive");
    statusText.textContent = "Monitoring";
  } else {
    indicator.classList.add("inactive");
    statusText.textContent = "Paused (after hours)";
  }

  if (data.lastChecked) {
    lastChecked.textContent = "Updated " + timeAgo(data.lastChecked);
  }

  countBadge.textContent = data.count;
  countBadge.hidden = false;

  if (fromCache && data.lastChecked) {
    cacheAge.textContent = "Last updated: " + timeAgo(data.lastChecked);
  }
}

function fetchStatus() {
  return fetch(API_BASE + "/api/status")
    .then(function (res) {
      return res.json();
    })
    .then(function (data) {
      if (data.offline) {
        return loadFromDb().then(function (cached) {
          if (cached) {
            renderDogs(cached.dogs);
            updateStatus(cached, true);
          }
        });
      }

      renderDogs(data.dogs);
      updateStatus(data, false);
      return saveToDb(data);
    })
    .catch(function () {
      return loadFromDb().then(function (cached) {
        if (cached) {
          renderDogs(cached.dogs);
          updateStatus(cached, true);
        }
      });
    });
}

function startPolling() {
  fetchStatus();
  pollTimer = setInterval(fetchStatus, POLL_INTERVAL);
}

function handleOnlineOffline() {
  if (navigator.onLine) {
    document.body.classList.remove("offline");
    fetchStatus();
  } else {
    document.body.classList.add("offline");
  }
}

function urlBase64ToUint8Array(base64String) {
  var padding = "=".repeat((4 - (base64String.length % 4)) % 4);
  var base64 = (base64String + padding).replace(/-/g, "+").replace(/_/g, "/");
  var raw = atob(base64);
  var arr = new Uint8Array(raw.length);
  for (var i = 0; i < raw.length; i++) {
    arr[i] = raw.charCodeAt(i);
  }
  return arr;
}

function initSubscribeButton() {
  var btn = document.getElementById("subscribe-btn");

  if (!("serviceWorker" in navigator) || !("PushManager" in window)) {
    return;
  }

  btn.hidden = false;

  navigator.serviceWorker.ready.then(function (reg) {
    reg.pushManager.getSubscription().then(function (sub) {
      if (sub) {
        btn.textContent = "Unsubscribe";
        btn.classList.add("subscribed");
      }
    });
  });

  btn.addEventListener("click", function () {
    navigator.serviceWorker.ready.then(function (reg) {
      reg.pushManager.getSubscription().then(function (sub) {
        if (sub) {
          unsubscribe(sub, btn);
        } else {
          subscribe(reg, btn);
        }
      });
    });
  });
}

function subscribe(reg, btn) {
  fetch(API_BASE + "/api/vapid-public-key")
    .then(function (res) {
      if (!res.ok) {
        throw new Error("Failed to fetch VAPID key: " + res.status);
      }
      return res.text();
    })
    .then(function (key) {
      return reg.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: urlBase64ToUint8Array(key.replace(/"/g, "")),
      });
    })
    .then(function (sub) {
      var subJson = sub.toJSON();
      return fetch(API_BASE + "/api/subscribe", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          endpoint: sub.endpoint,
          keys: {
            p256dh: subJson.keys.p256dh,
            auth: subJson.keys.auth,
          },
        }),
      });
    })
    .then(function () {
      btn.textContent = "Unsubscribe";
      btn.classList.add("subscribed");
    });
}

function unsubscribe(sub, btn) {
  var subJson = sub.toJSON();
  fetch(API_BASE + "/api/subscribe", {
    method: "DELETE",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      endpoint: sub.endpoint,
      keys: {
        p256dh: subJson.keys.p256dh,
        auth: subJson.keys.auth,
      },
    }),
  })
    .then(function () {
      return sub.unsubscribe();
    })
    .then(function () {
      btn.textContent = "Subscribe";
      btn.classList.remove("subscribed");
    });
}

document.getElementById("modal-close").addEventListener("click", closeModal);
document.getElementById("modal-overlay").addEventListener("click", function (e) {
  if (e.target === this) {
    closeModal();
  }
});
document.addEventListener("keydown", function (e) {
  if (e.key === "Escape") {
    closeModal();
  }
});

window.addEventListener("online", handleOnlineOffline);
window.addEventListener("offline", handleOnlineOffline);

if (!navigator.onLine) {
  document.body.classList.add("offline");
}

if ("serviceWorker" in navigator) {
  navigator.serviceWorker.register("/sw.js");
}

initSubscribeButton();
startPolling();
