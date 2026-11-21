(function () {
    'use strict';

    var wrap = document.getElementById('notifWrap');
    if (!wrap) return;

    var btn = document.getElementById('notifBtn');
    var badge = document.getElementById('notifBadge');
    var panel = document.getElementById('notifPanel');
    var list = document.getElementById('notifList');
    var markAll = document.getElementById('notifMarkAll');

    function esc(s) {
        return String(s == null ? '' : s)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;')
            .replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    function setBadge(n) {
        if (n > 0) {
            badge.textContent = n > 99 ? '99+' : n;
            badge.style.display = '';
        } else {
            badge.style.display = 'none';
        }
    }

    function itemHtml(n) {
        var color = n.type === 'trip_cancelled' ? '#E53935'
                  : n.type === 'trip_delayed' ? '#FB8C00'
                  : n.type === 'coupon' ? '#2E7D32' : '#1A73E8';
        var unread = n.unread ? ' notif-item-unread' : '';
        return '<a class="notif-item' + unread + '" href="' + esc(n.linkUrl || '#') + '" data-id="' + n.id + '">'
            + '<span class="notif-dot" style="background:' + color + '"></span>'
            + '<span class="notif-body">'
            + '<span class="notif-type" style="color:' + color + '">' + esc(n.typeLabel) + '</span>'
            + '<span class="notif-title">' + esc(n.title) + '</span>'
            + '<span class="notif-text">' + esc(n.body) + '</span>'
            + '<span class="notif-time">' + esc(n.timeAgo) + '</span>'
            + '</span></a>';
    }

    function csrf() {
        var el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    var unreadCount = 0;

    function render(items) {
        if (!items || items.length === 0) {
            list.innerHTML = '<div class="notif-empty">Chưa có thông báo nào.</div>';
            return;
        }
        list.innerHTML = items.map(itemHtml).join('');
        Array.prototype.forEach.call(list.querySelectorAll('.notif-item'), function (a) {
            a.addEventListener('click', function () {
                var id = a.getAttribute('data-id');
                if (a.classList.contains('notif-item-unread')) {
                    unreadCount = Math.max(0, unreadCount - 1);
                    setBadge(unreadCount);
                }
                fetch('/Notification/MarkRead?id=' + id, {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': csrf() }
                });
            });
        });
    }

    function load() {
        fetch('/Notification/Feed', { headers: { 'X-Requested-With': 'fetch' } })
            .then(function (r) { return r.json(); })
            .then(function (d) {
                unreadCount = d.unread || 0;
                setBadge(unreadCount);
                render(d.items || []);
            })
            .catch(function () {
                list.innerHTML = '<div class="notif-empty">Không tải được thông báo.</div>';
            });
    }

    // Toggle panel
    btn.addEventListener('click', function (e) {
        e.stopPropagation();
        var open = panel.classList.toggle('open');
        if (open) load();
    });

    document.addEventListener('click', function (e) {
        if (!wrap.contains(e.target)) panel.classList.remove('open');
    });

    markAll.addEventListener('click', function (e) {
        e.stopPropagation();
        fetch('/Notification/MarkAllRead', {
            method: 'POST',
            headers: { 'RequestVerificationToken': csrf() }
        }).then(function () {
            unreadCount = 0;
            setBadge(0);
            load();
        });
    });

    // ===== SignalR: nhận thông báo tức thời =====
    function toast(n) {
        var color = n.type === 'trip_cancelled' ? '#E53935'
                  : n.type === 'trip_delayed' ? '#FB8C00'
                  : n.type === 'coupon' ? '#2E7D32' : '#1A73E8';
        var box = document.createElement('div');
        box.className = 'notif-toast';
        box.style.setProperty('--toast-color', color);
        box.innerHTML = '<div class="toast-type">' + esc(n.typeLabel) + '</div>'
            + '<div class="toast-title">' + esc(n.title) + '</div>'
            + '<div class="toast-text">' + esc(n.body) + '</div>'
            + (n.linkUrl ? '<a class="toast-link" href="' + esc(n.linkUrl) + '">Xem chi tiết →</a>' : '')
            + '<button class="toast-x" type="button">×</button>';
        document.body.appendChild(box);
        setTimeout(function () { box.classList.add('show'); }, 20);

        function close() {
            box.classList.remove('show');
            setTimeout(function () { if (box.parentNode) box.parentNode.removeChild(box); }, 320);
        }
        box.querySelector('.toast-x').addEventListener('click', close);
        setTimeout(close, 15000);
    }

    // Lấy unread ban đầu
    fetch('/Notification/UnreadCount')
        .then(function (r) { return r.json(); })
        .then(function (d) { unreadCount = d.unread || 0; setBadge(unreadCount); })
        .catch(function () { });

    if (window.signalR) {
        var conn = new signalR.HubConnectionBuilder()
            .withUrl('/hub/notifications')
            .withAutomaticReconnect([0, 2000, 5000, 10000, 20000])
            .build();

        conn.on('notify', function (n) {
            unreadCount = n.unreadCount || (unreadCount + 1);
            setBadge(unreadCount);
            toast(n);
            // Nếu panel đang mở, nạp lại để thấy thông báo mới nhất
            if (panel.classList.contains('open')) load();
        });

        function start() {
            conn.start().catch(function () { setTimeout(start, 5000); });
        }
        start();
    }
})();
