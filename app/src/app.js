var API_BASE =
  location.hostname === "localhost" || location.hostname === "127.0.0.1"
    ? "http://localhost:7071"
    : "";

var DB_NAME = "get-a-pet";
var DB_VERSION = 2;
var STORE_NAME = "status";
var POLL_INTERVAL = 30000;
var SORT_KEY = "khs-sort";
var SHELTER_FILTER_KEY = "shelter-filter";
var NOTIF_SHELTER_KEY = "notif-shelters";

var SHELTER_IDS = ["khs", "kcpp", "gpspca"];
var SHELTER_NAMES = { khs: "KHS", kcpp: "KC Pet Project", gpspca: "Great Plains SPCA" };

var pollTimer = null;
var MONITOR_INTERVAL = 60000;
var monitorTimer = null;

function triggerMonitor(force) {
  var url = API_BASE + "/api/monitor" + (force ? "?force=true" : "");
  fetch(url, { method: "POST" })
    .then(function (res) {
      if (res.ok) {
        return res.json().then(function (body) {
          if (!body.offline) {
            fetchStatus();
          }
        });
      }
      if (res.status === 500) {
        return res.json().then(function (problem) {
          setMonitorError(problem.detail || problem.title || "Monitor check failed");
        }).catch(function () {
          setMonitorError("Monitor check failed");
        });
      }
    })
    .catch(function () {});
}

function setMonitorError(message) {
  var indicator = document.getElementById("status-indicator");
  var statusText = document.getElementById("status-text");
  indicator.classList.remove("inactive");
  indicator.classList.add("error");
  statusText.textContent = "Monitor Error";
  console.error("Monitor error:", message);
}

function startMonitorTrigger() {
  triggerMonitor();
  monitorTimer = setInterval(triggerMonitor, MONITOR_INTERVAL);
}

function stopMonitorTrigger() {
  if (monitorTimer !== null) {
    clearInterval(monitorTimer);
    monitorTimer = null;
  }
}

document.addEventListener("visibilitychange", function () {
  if (document.visibilityState === "hidden") {
    stopMonitorTrigger();
  } else {
    startMonitorTrigger();
    fetchStatus();
  }
});
var currentDogs = null;
var currentAdoptedDogs = null;
var currentSort = localStorage.getItem(SORT_KEY) || "newest";
var currentPage = 1;

function loadActiveShelters() {
  var saved = localStorage.getItem(SHELTER_FILTER_KEY);
  if (!saved) {
    return new Set(SHELTER_IDS);
  }
  try {
    var parsed = JSON.parse(saved);
    return new Set(Array.isArray(parsed) ? parsed : SHELTER_IDS);
  } catch (e) {
    return new Set(SHELTER_IDS);
  }
}

var activeShelters = loadActiveShelters();

function loadActiveNotifShelters() {
  try {
    var saved = JSON.parse(localStorage.getItem(NOTIF_SHELTER_KEY) || "null");
    return new Set(Array.isArray(saved) ? saved : SHELTER_IDS);
  } catch (e) {
    return new Set(SHELTER_IDS);
  }
}

var activeNotifShelters = loadActiveNotifShelters();
var currentSubData = null;
var pendingSubReg = null;

function openDb() {
  return new Promise(function (resolve, reject) {
    var req = indexedDB.open(DB_NAME, DB_VERSION);
    req.onupgradeneeded = function () {
      if (!req.result.objectStoreNames.contains(STORE_NAME)) {
        req.result.createObjectStore(STORE_NAME);
      }
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
  currentDogs = dogs || null;

  var grid = document.getElementById("dog-grid");
  var empty = document.getElementById("empty-state");

  if (!dogs || dogs.length === 0) {
    grid.innerHTML = "";
    empty.hidden = false;
    return;
  }

  empty.hidden = true;
  grid.innerHTML = dogs
    .map(function (dog, index) {
      var imgSrc = dog.photoUrl || "";
      var imgTag = imgSrc
        ? '<img src="' + imgSrc + '" alt="' + (dog.name || "Dog") + '" loading="lazy">'
        : '<img src="" alt="No photo" style="background:var(--surface)">';
      var isNew = dog.firstSeen && (Date.now() - new Date(dog.firstSeen).getTime()) < 86400000;
      var breed = dog.breed ? dog.breed.replace(/\s*\([^)]+\)/g, "").replace(/\s*\/\s*Mix\s*$/i, "").trim() : null;
      var shelterName = SHELTER_NAMES[dog.shelterId] || dog.shelterId || null;
      var tags = [shelterName, dog.size, dog.weight].filter(Boolean);

      return (
        '<div class="dog-card" data-aid="' +
        dog.aid +
        '" style="--i: ' + Math.min(index, 15) + '">' +
        imgTag +
        (isNew ? '<span class="new-badge">New</span>' : '') +
        '<div class="dog-card-info">' +
        "<h3>" +
        (dog.name || "Unknown") +
        "</h3>" +
        "<p>" +
        [dog.gender, dog.age].filter(Boolean).join(" &middot; ") +
        "</p>" +
        (breed ? '<p class="dog-card-breed">' + breed + "</p>" : "") +
        (tags.length ? '<div class="dog-card-tags">' + tags.map(function (t) { return '<span class="dog-card-tag">' + t + "</span>"; }).join("") + "</div>" : "") +
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

function renderAdoptedDogs(dogs) {
  currentAdoptedDogs = dogs || null;

  var grid = document.getElementById("adopted-grid");
  var empty = document.getElementById("adopted-empty-state");
  var badge = document.getElementById("adopted-count-badge");

  if (!dogs || dogs.length === 0) {
    grid.innerHTML = "";
    empty.hidden = false;
    badge.hidden = true;
    return;
  }

  empty.hidden = true;
  badge.textContent = dogs.length;
  badge.hidden = false;

  var sorted = dogs.slice().sort(function (a, b) {
    return new Date(b.adoptedAt) - new Date(a.adoptedAt);
  });

  grid.innerHTML = sorted
    .map(function (dog, index) {
      var imgSrc = dog.photoUrl || "";
      var imgTag = imgSrc
        ? '<img src="' + imgSrc + '" alt="' + (dog.name || "Dog") + '" loading="lazy">'
        : '<img src="" alt="No photo" style="background:var(--surface)">';
      var breed = dog.breed ? dog.breed.replace(/\s*\([^)]+\)/g, "").replace(/\s*\/\s*Mix\s*$/i, "").trim() : null;
      var tags = [dog.size, dog.weight].filter(Boolean);

      return (
        '<div class="dog-card adopted-dog-card" data-aid="' +
        dog.aid +
        '" style="--i: ' + Math.min(index, 15) + '">' +
        imgTag +
        '<span class="adopted-badge">Adopted ' + timeAgo(dog.adoptedAt) + '</span>' +
        '<div class="dog-card-info">' +
        "<h3>" + (dog.name || "Unknown") + "</h3>" +
        "<p>" + [dog.gender, dog.age].filter(Boolean).join(" &middot; ") + "</p>" +
        (breed ? '<p class="dog-card-breed">' + breed + "</p>" : "") +
        (tags.length ? '<div class="dog-card-tags">' + tags.map(function (t) { return '<span class="dog-card-tag">' + t + "</span>"; }).join("") + "</div>" : "") +
        "</div></div>"
      );
    })
    .join("");

  grid.querySelectorAll(".adopted-dog-card").forEach(function (card) {
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
  if (dog.shelterId) {
    details.push({ label: "Shelter", value: SHELTER_NAMES[dog.shelterId] || dog.shelterId });
  }
  if (dog.gender) {
    details.push({ label: "Gender", value: dog.gender });
  }
  if (dog.age) {
    details.push({ label: "Age", value: dog.age });
  }
  if (dog.breed) {
    details.push({ label: "Breed", value: dog.breed });
  }
  if (dog.color) {
    details.push({ label: "Color", value: dog.color });
  }
  if (dog.size) {
    details.push({ label: "Size", value: dog.size });
  }
  if (dog.weight) {
    details.push({ label: "Weight", value: dog.weight });
  }
  var shelteredDate = dog.intakeDate || dog.listingDate;
  if (shelteredDate) {
    details.push({ label: "At shelter since", value: new Date(shelteredDate).toLocaleDateString(undefined, { year: "numeric", month: "short", day: "numeric" }) });
  }
  if (dog.adoptedAt) {
    details.push({ label: "Adopted", value: timeAgo(dog.adoptedAt) });
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

function renderPagination(page, pageSize, totalCount) {
  var container = document.getElementById("pagination");
  var prevBtn = document.getElementById("prev-btn");
  var nextBtn = document.getElementById("next-btn");
  var pageInfo = document.getElementById("page-info");
  var pageNums = document.getElementById("page-numbers");
  var totalPages = Math.ceil(totalCount / pageSize);

  if (totalPages <= 1) {
    container.hidden = true;
    return;
  }

  container.hidden = false;
  prevBtn.disabled = page <= 1;
  nextBtn.disabled = page >= totalPages;
  pageInfo.textContent = "Page " + page + " of " + totalPages;

  if (pageNums) {
    var buttons = [];
    var start = Math.max(1, page - 2);
    var end = Math.min(totalPages, page + 2);

    if (start > 1) {
      buttons.push('<button class="page-num" data-page="1">1</button>');
      if (start > 2) buttons.push('<span class="page-dots">...</span>');
    }

    for (var i = start; i <= end; i++) {
      buttons.push('<button class="page-num' + (i === page ? ' active' : '') + '" data-page="' + i + '">' + i + '</button>');
    }

    if (end < totalPages) {
      if (end < totalPages - 1) buttons.push('<span class="page-dots">...</span>');
      buttons.push('<button class="page-num" data-page="' + totalPages + '">' + totalPages + '</button>');
    }

    pageNums.innerHTML = buttons.join("");

    pageNums.querySelectorAll("[data-page]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        currentPage = parseInt(btn.dataset.page, 10);
        fetchStatus();
        window.scrollTo({ top: 0, behavior: "smooth" });
      });
    });
  }
}

function updateStatus(data, fromCache) {
  var indicator = document.getElementById("status-indicator");
  var statusText = document.getElementById("status-text");
  var statusPill = document.querySelector(".status-pill");
  var lastChecked = document.getElementById("last-checked");
  var countBadge = document.getElementById("count-badge");
  var cacheAge = document.getElementById("cache-age");

  indicator.classList.remove("error");
  if (data.isMonitoringActive) {
    indicator.classList.remove("inactive");
    statusText.textContent = "Monitoring";
    statusPill.classList.remove("status-pill--paused");
    statusPill.title = "";
  } else {
    indicator.classList.add("inactive");
    statusText.textContent = "Paused (after hours)";
    statusPill.classList.add("status-pill--paused");
    statusPill.title = "Click to force a check";
  }

  if (data.lastChecked) {
    lastChecked.textContent = "Updated " + timeAgo(data.lastChecked);
  }

  countBadge.textContent = data.totalCount;
  countBadge.hidden = false;

  if (fromCache && data.lastChecked) {
    cacheAge.textContent = "Last updated: " + timeAgo(data.lastChecked);
  }
}

function buildStatusUrl() {
  var shelters = Array.from(activeShelters);
  var params = new URLSearchParams({
    sort: currentSort,
    page: currentPage,
  });
  if (shelters.length < SHELTER_IDS.length) {
    params.set("shelters", shelters.join(","));
  }
  return API_BASE + "/api/status?" + params.toString();
}

function applyStatusData(data, fromCache) {
  renderDogs(data.dogs);
  renderAdoptedDogs(data.recentlyAdopted || []);
  updateStatus(data, fromCache);
  if (!fromCache) {
    renderPagination(data.page, data.pageSize, data.totalCount);
  }
}

function fetchStatus() {
  return fetch(buildStatusUrl())
    .then(function (res) {
      return res.json();
    })
    .then(function (data) {
      if (data.offline) {
        return loadFromDb().then(function (cached) {
          if (cached) {
            applyStatusData(cached, true);
          }
        });
      }

      applyStatusData(data, false);
      return saveToDb(data);
    })
    .catch(function () {
      return loadFromDb().then(function (cached) {
        if (cached) {
          applyStatusData(cached, true);
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

function showIosBanner() {
  if (localStorage.getItem("ios-banner-dismissed")) return;
  var banner = document.getElementById("ios-install-banner");
  banner.hidden = false;
  requestAnimationFrame(function () {
    requestAnimationFrame(function () {
      banner.classList.add("visible");
    });
  });
}

function closeNotifPanel() {
  var panel = document.getElementById("notif-panel");
  panel.hidden = true;
  document.getElementById("notif-panel-main").hidden = false;
  document.getElementById("notif-panel-confirm").hidden = true;
}

function toggleNotifPanel() {
  var panel = document.getElementById("notif-panel");
  if (panel.hidden) {
    panel.hidden = false;
  } else {
    closeNotifPanel();
  }
}

function openNotifSetupModal(reg) {
  pendingSubReg = reg;
  document.querySelectorAll(".notif-setup-check").forEach(function (check) {
    check.checked = activeShelters.has(check.value);
  });
  var anyChecked = document.querySelectorAll(".notif-setup-check:checked").length > 0;
  document.getElementById("notif-setup-confirm").disabled = !anyChecked;
  document.getElementById("notif-setup-overlay").classList.add("active");
}

function closeNotifSetupModal() {
  document.getElementById("notif-setup-overlay").classList.remove("active");
  pendingSubReg = null;
}

function initSubscribeButton() {
  var btn = document.getElementById("subscribe-btn");

  if (!("serviceWorker" in navigator) || !("PushManager" in window)) {
    var isIos = /iphone|ipad|ipod/i.test(navigator.userAgent);
    var isStandalone = window.navigator.standalone === true;
    if (isIos && !isStandalone) {
      showIosBanner();
    }
    return;
  }

  btn.hidden = false;

  navigator.serviceWorker.ready.then(function (reg) {
    reg.pushManager.getSubscription().then(function (sub) {
      if (sub) {
        btn.classList.add("subscribed");
        var subJson = sub.toJSON();
        currentSubData = { endpoint: sub.endpoint, keys: { p256dh: subJson.keys.p256dh, auth: subJson.keys.auth } };
        fetch(API_BASE + "/api/subscribe", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            endpoint: sub.endpoint,
            keys: { p256dh: subJson.keys.p256dh, auth: subJson.keys.auth },
            shelterIds: Array.from(activeNotifShelters),
          }),
        }).catch(function () {});
        showNotifShelterFilter();
      }
    });
  });

  btn.addEventListener("click", function () {
    navigator.serviceWorker.ready.then(function (reg) {
      reg.pushManager.getSubscription().then(function (sub) {
        if (sub) {
          toggleNotifPanel();
        } else {
          openNotifSetupModal(reg);
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
      currentSubData = { endpoint: sub.endpoint, keys: { p256dh: subJson.keys.p256dh, auth: subJson.keys.auth } };
      return fetch(API_BASE + "/api/subscribe", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          endpoint: sub.endpoint,
          keys: {
            p256dh: subJson.keys.p256dh,
            auth: subJson.keys.auth,
          },
          shelterIds: Array.from(activeNotifShelters),
        }),
      });
    })
    .then(function () {
      btn.classList.add("subscribed");
      showNotifShelterFilter();
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
      btn.classList.remove("subscribed");
      currentSubData = null;
      hideNotifShelterFilter();
    });
}

function initTabs() {
  var buttons = document.querySelectorAll(".tab-btn");
  buttons.forEach(function (btn) {
    btn.addEventListener("click", function () {
      if (btn.classList.contains("active")) return;
      buttons.forEach(function (b) {
        b.classList.remove("active");
      });
      btn.classList.add("active");
      var tab = btn.dataset.tab;
      document.getElementById("tab-available").hidden = (tab !== "available");
      document.getElementById("tab-adopted").hidden = (tab !== "adopted");
    });
  });
}

function initSortBar() {
  var buttons = document.querySelectorAll("[data-sort]");
  buttons.forEach(function (btn) {
    if (btn.dataset.sort === currentSort) {
      btn.classList.add("active");
    }
    btn.addEventListener("click", function () {
      if (btn.dataset.sort === currentSort) return;
      currentSort = btn.dataset.sort;
      currentPage = 1;
      localStorage.setItem(SORT_KEY, currentSort);
      buttons.forEach(function (b) {
        b.classList.toggle("active", b.dataset.sort === currentSort);
      });
      fetchStatus();
    });
  });
}

function initShelterFilter() {
  var buttons = document.querySelectorAll("[data-shelter]");
  buttons.forEach(function (btn) {
    var shelterId = btn.dataset.shelter;
    btn.classList.toggle("active", activeShelters.has(shelterId));
    btn.addEventListener("click", function () {
      if (activeShelters.has(shelterId)) {
        if (activeShelters.size > 1) {
          activeShelters.delete(shelterId);
          btn.classList.remove("active");
        }
      } else {
        activeShelters.add(shelterId);
        btn.classList.add("active");
      }
      localStorage.setItem(SHELTER_FILTER_KEY, JSON.stringify(Array.from(activeShelters)));
      currentPage = 1;
      fetchStatus();
    });
  });
}

function updateNotifPreferences() {
  if (!currentSubData) return;
  fetch(API_BASE + "/api/subscribe", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      endpoint: currentSubData.endpoint,
      keys: currentSubData.keys,
      shelterIds: Array.from(activeNotifShelters),
    }),
  }).catch(function () {});
}

function showNotifShelterFilter() {
  document.querySelectorAll("[data-notif-shelter]").forEach(function (btn) {
    btn.classList.toggle("active", activeNotifShelters.has(btn.dataset.notifShelter));
  });
}

function hideNotifShelterFilter() {
  closeNotifPanel();
}

function initNotifShelterFilter() {
  var buttons = document.querySelectorAll("[data-notif-shelter]");
  buttons.forEach(function (btn) {
    btn.addEventListener("click", function () {
      var shelterId = btn.dataset.notifShelter;
      if (activeNotifShelters.has(shelterId)) {
        if (activeNotifShelters.size > 1) {
          activeNotifShelters.delete(shelterId);
          btn.classList.remove("active");
        }
      } else {
        activeNotifShelters.add(shelterId);
        btn.classList.add("active");
      }
      localStorage.setItem(NOTIF_SHELTER_KEY, JSON.stringify(Array.from(activeNotifShelters)));
      updateNotifPreferences();
    });
  });
}

document.querySelector(".status-pill").addEventListener("click", function () {
  var pill = this;
  if (!pill.classList.contains("status-pill--paused")) return;
  var statusText = document.getElementById("status-text");
  pill.classList.remove("status-pill--paused");
  statusText.textContent = "Checking\u2026";
  triggerMonitor(true);
  setTimeout(function () {
    if (!pill.classList.contains("status-pill--paused")) return;
    statusText.textContent = "Paused (after hours)";
    pill.classList.add("status-pill--paused");
  }, 10000);
});

document.getElementById("ios-banner-dismiss").addEventListener("click", function () {
  var banner = document.getElementById("ios-install-banner");
  banner.classList.remove("visible");
  setTimeout(function () { banner.hidden = true; }, 500);
  localStorage.setItem("ios-banner-dismissed", "1");
});

document.getElementById("prev-btn").addEventListener("click", function () {
  if (currentPage > 1) {
    currentPage--;
    fetchStatus();
    window.scrollTo({ top: 0, behavior: "smooth" });
  }
});

document.getElementById("next-btn").addEventListener("click", function () {
  currentPage++;
  fetchStatus();
  window.scrollTo({ top: 0, behavior: "smooth" });
});

document.getElementById("unsubscribe-btn").addEventListener("click", function () {
  document.getElementById("notif-panel-main").hidden = true;
  document.getElementById("notif-panel-confirm").hidden = false;
});

document.getElementById("unsubscribe-cancel-btn").addEventListener("click", function () {
  document.getElementById("notif-panel-main").hidden = false;
  document.getElementById("notif-panel-confirm").hidden = true;
});

document.getElementById("unsubscribe-confirm-btn").addEventListener("click", function () {
  navigator.serviceWorker.ready.then(function (reg) {
    reg.pushManager.getSubscription().then(function (sub) {
      if (sub) {
        var btn = document.getElementById("subscribe-btn");
        unsubscribe(sub, btn);
      }
    });
  });
});

document.addEventListener("click", function (e) {
  var group = document.querySelector(".notif-group");
  var panel = document.getElementById("notif-panel");
  if (group && !group.contains(e.target) && !panel.hidden) {
    closeNotifPanel();
  }
});

document.getElementById("modal-close").addEventListener("click", closeModal);
document.getElementById("modal-overlay").addEventListener("click", function (e) {
  if (e.target === this) {
    closeModal();
  }
});
document.addEventListener("keydown", function (e) {
  if (e.key === "Escape") {
    closeModal();
    var panel = document.getElementById("notif-panel");
    if (!panel.hidden) {
      closeNotifPanel();
    }
    var setupOverlay = document.getElementById("notif-setup-overlay");
    if (setupOverlay.classList.contains("active")) {
      closeNotifSetupModal();
    }
  }
});

document.getElementById("notif-setup-confirm").addEventListener("click", function () {
  if (!pendingSubReg) return;
  var selected = [];
  document.querySelectorAll(".notif-setup-check:checked").forEach(function (check) {
    selected.push(check.value);
  });
  if (selected.length === 0) return;
  activeNotifShelters = new Set(selected);
  localStorage.setItem(NOTIF_SHELTER_KEY, JSON.stringify(selected));
  var btn = document.getElementById("subscribe-btn");
  subscribe(pendingSubReg, btn);
  closeNotifSetupModal();
});

document.getElementById("notif-setup-cancel").addEventListener("click", closeNotifSetupModal);

document.getElementById("notif-setup-overlay").addEventListener("click", function (e) {
  if (e.target === this) {
    closeNotifSetupModal();
  }
});

document.querySelectorAll(".notif-setup-check").forEach(function (check) {
  check.addEventListener("change", function () {
    var anyChecked = document.querySelectorAll(".notif-setup-check:checked").length > 0;
    document.getElementById("notif-setup-confirm").disabled = !anyChecked;
  });
});

window.addEventListener("online", handleOnlineOffline);
window.addEventListener("offline", handleOnlineOffline);

if (!navigator.onLine) {
  document.body.classList.add("offline");
}

if ("serviceWorker" in navigator) {
  var prevController = navigator.serviceWorker.controller;
  navigator.serviceWorker.register("/sw.js");
  navigator.serviceWorker.addEventListener("controllerchange", function () {
    if (prevController) {
      location.reload();
    }
  });
}

initTabs();
initSortBar();
initShelterFilter();
initNotifShelterFilter();
initSubscribeButton();
startPolling();
startMonitorTrigger();
